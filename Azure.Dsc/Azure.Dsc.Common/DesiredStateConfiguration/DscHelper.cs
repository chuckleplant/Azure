using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using ICSharpCode.SharpZipLib.Zip;

namespace Azure.Dsc.Common.DesiredStateConfiguration
{
    public static class DscHelper
    {
        public static string PrepareDscResourceModules(string tempModulePath, Stream stream)
        {
            var psModulePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles) +
                                    @"\WindowsPowerShell\Modules\";
            stream.Seek(0, SeekOrigin.Begin);

            var zipper = new FastZip();

            zipper.ExtractZip(stream, tempModulePath, FastZip.Overwrite.Always, null, null, null, false, true);

            return psModulePath;
        }

        public static string PrepareDscResourcePackages(string tempModulePath, Stream stream)
        {
            var dscTargetPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles) +
                                    @"\WindowsPowerShell\DscService\Modules\";

            if (!Directory.Exists(dscTargetPath))
            {
                Directory.CreateDirectory(dscTargetPath);
            }

            stream.Seek(0, SeekOrigin.Begin);

            var zipper = new FastZip();

            zipper.ExtractZip(stream, tempModulePath, FastZip.Overwrite.Always, null, null, null, false, true);

            var path = tempModulePath + @"Modules\";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            foreach (var directory in Directory.GetDirectories(path))
            {
                string directoryName = directory.Substring(directory.LastIndexOf('\\') + 1);
                string moduleVersion = "<version>";
                using (
                    FileStream psd1 = new FileStream(directory + "\\" + directoryName + ".psd1", FileMode.Open, FileAccess.Read)
                    )
                {
                    StreamReader reader = new StreamReader(psd1);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("ModuleVersion"))
                        {
                            moduleVersion = line.Split('=')[1].Trim().Trim('\'');
                            break;
                        }
                    }
                }

                var zipFilePath = tempModulePath + directoryName + "_" + moduleVersion + ".zip";
                zipper.CreateZip(zipFilePath, directory, true, null);

                NewCheckSumFile(zipFilePath);

                var targetZipFilePath = dscTargetPath + directoryName + "_" + moduleVersion + ".zip";
                File.Copy(zipFilePath, targetZipFilePath, true);
                File.Copy(zipFilePath + ".checksum", targetZipFilePath + ".checksum", true);
            }
            return path;
        }

        private static byte[] GetHash(string algorithm, string filePath)
        {
            byte[] hash = null;
            using (var fileStream = File.OpenRead(filePath))
            {
                var hasher = HashAlgorithm.Create(algorithm);
                hash = hasher.ComputeHash(fileStream);
                fileStream.Close();
            }

            return hash;
        }

        private static void NewCheckSumFile(string zipFilePath)
        {
            var hash = GetHash("SHA256", zipFilePath);

            var hashString = BitConverter.ToString(hash).Replace("-", string.Empty);

            File.WriteAllText(zipFilePath + ".checksum", hashString);
        } 
        public static void MoveDscModules(string path)
        {
            var psModulePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles) +
                               @"\WindowsPowerShell\Modules\";

            try
            {
                foreach (var directory in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                {
                    var targetDirectory = directory.Replace(path, psModulePath);
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }
                }

                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    File.Move(file, file.Replace(path, psModulePath));
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Fix this man! {0}", ex.Message);
            }
        }
    }
}
