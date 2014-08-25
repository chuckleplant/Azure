using System;
using System.Diagnostics;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Azure.Dsc.Common.Security
{
    public static class Secret
    {
        public static string Decrypt(string value)
        {
            try
            {
                byte[] data = Convert.FromBase64String(value);
                EnvelopedCms cms = new EnvelopedCms();
                cms.Decode(data);

                var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                cms.Decrypt(store.Certificates);
                return Encoding.UTF8.GetString(cms.Encode());
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
            return string.Empty;
        }

        public static string Encrypt(string thumbprint, string value)
        {
            X509Certificate2 cert = GetCertificate(thumbprint);
            if (null != cert)
            {
                byte[] data = Encoding.UTF8.GetBytes(value);
                ContentInfo contentInfo = new ContentInfo(data);
                EnvelopedCms cms = new EnvelopedCms(contentInfo);
                CmsRecipient recipient = new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber, cert);
                cms.Encrypt(recipient);
                return Convert.ToBase64String(cms.Encode());
            }
            return string.Empty;

        }

        private static X509Certificate2 GetCertificate(string thumbprint)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (certs.Count > 0) return certs[0];
            }
            finally
            {
                store.Close();
            }
            return null;
        }
    }
}
