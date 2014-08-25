using System;
using System.Configuration;
using Microsoft.WindowsAzure;
using Sogeti.IaC.Common.Security;

namespace Azure.Dsc.Common.Security
{
    public class Impersonator : IDisposable
    {
        public Win32Logon User { get; private set; }

        public Impersonator()
        {
            User = new Win32Logon();
            string username = CloudConfigurationManager.GetSetting("Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername");
            string passhash = CloudConfigurationManager.GetSetting("Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(passhash))
            {
                throw new ConfigurationErrorsException(
                    "The Impersonator is specifically written for use in Microsoft Azure Cloud Services and requires that Remote Access is configured.");
            }

            string password = Secret.Decrypt(passhash);

            User.Logon(username, password, Environment.MachineName);
//            User.LoadProfile();
        }

        public static Impersonator Impersonate()
        {
            return new Impersonator();
        }

        ~Impersonator()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (User != null)
            {
//                User.UnloadProfile();
                User.Logout();
            }
        }
    }
}
