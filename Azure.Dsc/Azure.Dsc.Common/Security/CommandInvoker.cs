using System;
using System.Diagnostics;

namespace Azure.Dsc.Common.Security
{
    public class CommandInvoker : IDisposable
    {
        private Win32Logon User { get; set; }

        public CommandInvoker()
        {
            User = new Win32Logon();
        }

        ~CommandInvoker()
        {
            Dispose();
        }

        public virtual bool Execute(string command, string arguments)
        {
            bool completed = false;
            try
            {
                using (Impersonator.Impersonate())
                {
                    completed = InvokeCommand(command, arguments);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                completed = false;
            }

            return completed;
        }

        private bool InvokeCommand(string command, string arguments)
        {
            bool completed = false;
            try
            {
                string cmdline = string.Format("\"{0}\" {1}", command, arguments);

                if (User.UserLogged)
                {
                    Win32Process.CreateProcessAsUser(User, cmdline, true);
                    completed = true;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }

            return completed;
        }

        public void Dispose()
        {
            User.Dispose();
            User = null;
        }
    }
}
