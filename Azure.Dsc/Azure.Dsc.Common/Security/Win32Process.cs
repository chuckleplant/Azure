using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Azure.Dsc.Common.Security
{
    public class Win32Process
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessID;
            public Int32 dwThreadID;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public Int32 Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        private const int GENERIC_ALL_ACCESS = 0x10000000;

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32.dll", EntryPoint = "CreateProcess", SetLastError = true)]
        private static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandle,
            Int32 dwCreationFlags,
            IntPtr lpEnvrionment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            ref PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true)]
        private static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandle,
            Int32 dwCreationFlags,
            IntPtr lpEnvrionment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            ref PROCESS_INFORMATION lpProcessInformation);

        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static private extern int WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);


        public static void CreateProcess(string cmdline, bool wait = false)
        {
            IntPtr token = WindowsIdentity.GetCurrent().Token;
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            try
            {
                SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                sa.Length = Marshal.SizeOf(sa);

                STARTUPINFO si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.lpDesktop = "winsta0\\default";

                IntPtr lpEnvironment;
                if (!CreateEnvironmentBlock(out lpEnvironment, token, false))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateEnvironmentBlock");

                Trace.TraceInformation("CreateProcess - Cmdline: ", cmdline);

                if (!CreateProcess(
                                null,
                                cmdline,
                                ref sa, ref sa,
                                false, 0x00000400, lpEnvironment,
                                null, ref si, ref pi))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateProcessAsUser");

                // block until process exit
                if (wait) WaitForSingleObject(pi.hProcess, int.MaxValue);
            }
            finally
            {
                if (pi.hProcess != IntPtr.Zero)
                    CloseHandle(pi.hProcess);
                if (pi.hThread != IntPtr.Zero)
                    CloseHandle(pi.hThread);
            }
        }

        public static void CreateProcessAsUser(Win32Logon logon, string cmdline, bool wait = false)
        {
            if (!logon.UserLogged)
                throw new InvalidOperationException("User not logged-on.");

            IntPtr token = logon._impersonate;
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            try
            {
                SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                sa.Length = Marshal.SizeOf(sa);

                STARTUPINFO si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.lpDesktop = "winsta0\\default";

                IntPtr lpEnvironment;
                if (!CreateEnvironmentBlock(out lpEnvironment, token, false))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateEnvironmentBlock");

                Trace.TraceInformation("CreateProcessAsUser - Cmdline: ", cmdline);

                if (!CreateProcessAsUser(
                                    token,
                                    null,
                                    cmdline,
                                    ref sa, ref sa,
                                    false, 0x00000400, lpEnvironment,
                                    null, ref si, ref pi))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateProcessAsUser");

                // block until process exit
                if (wait) WaitForSingleObject(pi.hProcess, int.MaxValue);
            }
            finally
            {
                if (pi.hProcess != IntPtr.Zero)
                    CloseHandle(pi.hProcess);
                if (pi.hThread != IntPtr.Zero)
                    CloseHandle(pi.hThread);
            }
        }
    }
}
