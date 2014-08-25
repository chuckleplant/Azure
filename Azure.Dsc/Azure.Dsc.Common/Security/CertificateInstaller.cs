using System.IO;
using System.Security.Cryptography.X509Certificates;
using Azure.Dsc.Common.Security;

namespace Sogeti.IaC.Common.Security
{
    public class CertificateInstaller
    {
        public static bool Install(X509Certificate2 certificate)
        {
            string tempFile = Path.GetTempFileName();

            using (var byteStream = new MemoryStream(certificate.RawData))
            {
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    byteStream.WriteTo(fileStream);
                    fileStream.Flush();
                    fileStream.Close();
                }
                byteStream.Close();
            }

            var invoker = new CommandInvoker();



            return invoker.Execute("certutil", string.Format("-f -v -addStore -enterprise Root \"{0}\"", tempFile));
        }
    }
}
