using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Azure.Dsc.Common.Security
{
    public class Win32Logon : IDisposable
    {
        // registry subkeys
        const string SubKeyRun = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        const string SubKeyRunOnce = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
        const string SubKeyWinlogon = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";

        internal IntPtr _token;
        internal IntPtr _impersonate;
        PROFILEINFO _profile;
        string _username;

        ~Win32Logon()
        {
            Dispose();
        }

        public void Dispose()
        {
            UnloadProfile();
            Logout();
        }

        #region Reboot
        const uint SE_PRIVILEGE_DISABLED = 0x00000000;
        const uint SE_PRIVILEGE_ENABLED = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public uint HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGE
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privilege;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, [In, Out] ref LUID Luid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(
            IntPtr tokenHndl, bool diasableAll,
            [In] ref TOKEN_PRIVILEGE newTokenState,
            int length,
            IntPtr prevTokenState,
            IntPtr retLength);

        [DllImport("advapi32.dll", EntryPoint = "InitiateSystemShutdown", SetLastError = true)]
        private static extern bool InitiateSystemShutdown(
            string lpMachineName,
            string lpMessage,
            int dwTimeout,
            bool bForceAppsClosed,
            bool bRebootAfterShutdown);

        public static void Reboot()
        {
            LUID luid = new LUID();
            if (!LookupPrivilegeValue("", "SeShutdownPrivilege", ref luid))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "LookupPrivilegeValue");

            TOKEN_PRIVILEGE newPriv = new TOKEN_PRIVILEGE()
            {
                PrivilegeCount = 1,
                Privilege = new LUID_AND_ATTRIBUTES() { Luid = luid, Attributes = SE_PRIVILEGE_ENABLED }
            };
            if (!AdjustTokenPrivileges(WindowsIdentity.GetCurrent().Token, false, ref newPriv, Marshal.SizeOf(newPriv), IntPtr.Zero, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "AdjustTokenPrivileges");

            if (!InitiateSystemShutdown(null, null, 0, true, true))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "ExitWindowsEx");
        }
        #endregion

        #region Logon / Logout
        // Declare the logon types as constants
        const long LOGON32_LOGON_INTERACTIVE = 2;
        const long LOGON32_LOGON_NETWORK = 3;

        // Declare the logon providers as constants
        const long LOGON32_PROVIDER_DEFAULT = 0;
        const long LOGON32_PROVIDER_WINNT50 = 3;
        const long LOGON32_PROVIDER_WINNT40 = 2;
        const long LOGON32_PROVIDER_WINNT35 = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public Int32 Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        private enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        private enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        private const int GENERIC_ALL_ACCESS = 0x10000000;

        [DllImport("advapi32.dll", EntryPoint = "LogonUser")]
        private static extern bool LogonUser(
            string lpszUsername,
            string lpszDomain, 
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            ref IntPtr phToken);

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        private static extern bool DuplicateTokenEx(
            IntPtr hExistingToken,
            Int32 dwDesiredAccess,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            Int32 ImpersonationLevel,
            Int32 dwTokenType,
            ref IntPtr phNewToken);

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        public void Logon(string username, string password, string domain)
        {
            // already logged-on
            if (UserLogged)
                throw new InvalidOperationException("User already logged-on.");

            // store for later (LoadProfile)
            _username = username;

            // logon user
            if (!LogonUser(
                username,
                domain,
                password,
                (int)LOGON32_LOGON_INTERACTIVE,
                (int)LOGON32_PROVIDER_DEFAULT,
                ref _token))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "LogonUser");

            // duplicate primary token
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.Length = Marshal.SizeOf(sa);
            if (!DuplicateTokenEx(
                _token,
                GENERIC_ALL_ACCESS,
                ref sa,
                (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                (int)TOKEN_TYPE.TokenPrimary,
                ref _impersonate))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "DuplicateTokenEx");
        }

        public void Logout()
        {
            if (IntPtr.Zero != _impersonate)
            {
                CloseHandle(_impersonate);
                _impersonate = IntPtr.Zero;
            }

            if (IntPtr.Zero != _token)
            {
                CloseHandle(_token);
                _token = IntPtr.Zero;
            }
        }

        public bool UserLogged
        {
            get { return !((IntPtr.Zero == _token) || (IntPtr.Zero == _impersonate) || (string.IsNullOrEmpty(_username))); }
        }
        #endregion

        #region LoadProfile / UnloadProfile
        [StructLayout(LayoutKind.Sequential)]
        struct PROFILEINFO
        {
            public int dwSize;
            public int dwFlags;
            [MarshalAs(UnmanagedType.LPTStr)]
            public String lpUserName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public String lpProfilePath;
            [MarshalAs(UnmanagedType.LPTStr)]
            public String lpDefaultPath;
            [MarshalAs(UnmanagedType.LPTStr)]
            public String lpServerName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public String lpPolicyPath;
            public IntPtr hProfile;
        }

        [DllImport("userenv.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool LoadUserProfile(IntPtr hToken, ref PROFILEINFO lpProfileInfo);

        [DllImport("userenv.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnloadUserProfile(IntPtr hToken, ref PROFILEINFO lpProfileInfo);

        public void LoadProfile()
        {
            // already loaded
            if (ProfileLoaded)
                throw new InvalidOperationException("User profile already loaded.");

            // not logged-on
            if (!UserLogged)
                throw new InvalidOperationException("User not logged-on.");

            // load profile
            _profile.dwSize = Marshal.SizeOf(_profile);
            _profile.lpUserName = _username;
            _profile.dwFlags = 1;
            if (!LoadUserProfile(_impersonate, ref _profile))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "LoadUserProfile");
        }

        public void UnloadProfile()
        {
            if (0 != _profile.dwSize)
            {
                UnloadUserProfile(_impersonate, ref _profile);
                _profile = new PROFILEINFO();
            }
        }

        public bool ProfileLoaded
        {
            get { return ((0 != _profile.dwSize) && (IntPtr.Zero != _profile.hProfile)); }
        }
        #endregion

        private RegistryKey GetHKCU()
        {
            // profile not loaded
            // assume current user
            if (!ProfileLoaded)
                return Registry.CurrentUser;

            // use loaded profile
            return RegistryKey.FromHandle(new SafeRegistryHandle(_profile.hProfile, false));
        }

        public string GetAutoRun(string key, bool runonce = true)
        {
            string subkey = runonce ? SubKeyRunOnce : SubKeyRun;

            try
            {
                // read reg key
                using (RegistryKey regkey = GetHKCU().OpenSubKey(subkey))
                    return (string)regkey.GetValue(key);
            }
            catch { }
            return string.Empty;
        }

        public void SetAutoRun(string key, string value, bool runonce = true)
        {
            string subkey = runonce ? SubKeyRunOnce : SubKeyRun;

            // delete reg key ?
            if (string.IsNullOrEmpty(value))
            {
                using (RegistryKey regkey = GetHKCU().CreateSubKey(subkey))
                    regkey.DeleteValue(key, false);
                return;
            }

            // change reg value
            using (RegistryKey regkey = GetHKCU().CreateSubKey(subkey))
                regkey.SetValue(key, value, RegistryValueKind.String);
        }

        public static void SetAutoLogon(string username, string password, string domain)
        {
            using (RegistryKey regkey = Registry.LocalMachine.OpenSubKey(SubKeyWinlogon, true))
            {
                regkey.SetValue("AutoAdminLogon", "1");
                regkey.SetValue("DefaultUserName", username);
                regkey.SetValue("DefaultPassword", password);
                regkey.SetValue("DefaultDomainName", domain);
            }
        }
    }
}
