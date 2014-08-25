using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Azure.Dsc.Common.DesiredStateConfiguration;

namespace Sogeti.IaC.Model.DesiredStateConfiguration
{
    public static class DscParser
    {
        private static IEnumerable<DscResource> GetResources(string psModulePath)
        {
            var configurations = new List<DscResource>();

            foreach (var directory in Directory.GetDirectories(psModulePath, "*", SearchOption.TopDirectoryOnly))
            {
                if (!Directory.Exists(directory + "\\DSCResources"))
                {
                    continue;
                }

                Console.WriteLine(@"Analyzing module '{0}'", directory.Substring(directory.LastIndexOf('\\') + 1));
                Console.WriteLine(@"-----------------------------------------------------------------------------");

                foreach (
                    var resource in Directory.GetDirectories(directory + "\\DSCResources", "*", SearchOption.TopDirectoryOnly))
                {
                    DscResource dscResource = new DscResource();
                    foreach (var schema in Directory.GetFiles(resource, "*.schema.mof"))
                    {
                        Console.Write(@"- Analyzing resource '{0}'", resource.Substring(resource.LastIndexOf('\\') + 1));

                        var friendlyName = "";
                        using (var stream = File.OpenRead(schema))
                        {
                            var reader = new StreamReader(stream);
                            string line = "";
                            DscResourceClass dscClass = null;
                            while ((line = reader.ReadLine()) != null)
                            {
                                #region Friendly Name

                                if (line.Contains("FriendlyName"))
                                {
                                    var pattern = @"^.*FriendlyName\(\""(?<friendly>.*)\""\)\]\s*$";
                                    var regex = new Regex(pattern);
                                    var matches = regex.Matches(line);
                                    friendlyName = matches[0].Groups["friendly"].Value;
                                }

                                Console.Write(@".");

                                #endregion

                                #region Dsc Resource Class

                                if (line.ToLower().StartsWith("class"))
                                {
                                    dscClass = new DscResourceClass();

                                    dscClass.Name = Regex.Match(line, @"\w*_\w*").Value;
                                    dscClass.IsOmiBaseResource = line.ToLower().Contains("omi_baseresource");
                                    continue;
                                }

                                Console.Write(@".");

                                if (Regex.IsMatch(line, @"\[.*\;$"))
                                {
                                    var property = new DscResourceProperty();
                                    var attributeLine =
                                        Regex.Match(line, @"(?<attributeLine>\[.*\]).+\;$").Groups["attributeLine"].Value;
                                    var typeAndName = line.Replace(attributeLine, string.Empty).Trim().Split(' ');
                                    property.Type = typeAndName[0];
                                    property.Name = typeAndName[1].TrimEnd(';');

                                    var valueMap = Regex.Match(attributeLine,
                                        @"ValueMap\{(\""(?<values>\w+)\""[,]?\s?)*\}");
                                    if (valueMap.Success)
                                    {
                                        string[] values = new string[valueMap.Groups["values"].Captures.Count];
                                        for (int i = 0; i < values.Length; i++)
                                        {
                                            values[i] = valueMap.Groups["values"].Captures[i].Value;
                                        }

                                        property.ValidationSet = values;
                                    }

                                    var attribute = Regex.Match(attributeLine, @"^\[(?<attribute>\w*)[,]?[^\]]*\]\s*$");
                                    var attributeName = attribute.Groups["attribute"].Value;

                                    DscResourcePropertyAttribute attr;
                                    if (!Enum.TryParse(attributeName, true, out attr))
                                    {
                                        throw new Exception(string.Format("Attribute [{0}] could not be parsed",
                                            attributeName));
                                    }

                                    property.Attribute = attr;

                                    var embeddedInstance = Regex.Match(attributeLine,
                                        @"EmbeddedInstance\(\""(?<instance>[^\""]+)\""\)");

                                    if (embeddedInstance.Success)
                                    {
                                        property.EmbeddedInstance = embeddedInstance.Groups["instance"].Value;
                                    }

                                    dscClass.Properties.Add(property);
                                    continue;
                                }

                                Console.Write(@".");

                                if (Regex.IsMatch(line, @"\}\;"))
                                {
                                    dscResource.Classes.Add(dscClass);
                                }

                                Console.Write(@".");

                                #endregion
                            }
                        }

                        dscResource.ModulePath = directory;
                        dscResource.ResourcePath = resource;
                        dscResource.FriendlyName = friendlyName;

                        Console.Write(@".");

                        configurations.Add(dscResource);

                        Console.WriteLine(System.Environment.NewLine);
                    }
                }
            }

            return configurations;
        }

        public static IEnumerable<DscResource> GetAllDscResources()
        {
            var configurations = new List<DscResource>();

            var dscStandardResourcesModulePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.System) +
                                    @"\WindowsPowerShell\v1.0\Modules\";

            configurations.AddRange(GetResources(dscStandardResourcesModulePath));

            var dscResourceKitModulePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles) +
                                    @"\WindowsPowerShell\Modules\";

            configurations.AddRange(GetResources(dscResourceKitModulePath));

            return configurations;
        }

        public static DscResourceCache CacheResources(IEnumerable<DscResource> resources)
        {
            DscResourceCache.Instance.Clear();

            foreach (var resource in resources)
            {
                DscResourceCache.Instance[resource.ToString()] = resource;
            }

            return DscResourceCache.Instance;
        }
    }
}
