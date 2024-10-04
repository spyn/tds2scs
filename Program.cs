using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace tds2scs
{
    internal class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly IConfigurationRoot Config = GetAppSettings();

        static void Main(string[] args)
        {
            var solutionFile = !string.IsNullOrEmpty(Config["solutionFile"]) ? Config["solutionFile"] : String.Empty;
            var helixModule = Config["helixModule"];

            foreach (string arg in args)
            {
                if (arg.EndsWith(".sln"))
                {
                    solutionFile = arg;
                }
            }

            if (string.IsNullOrEmpty(solutionFile) && !File.Exists(solutionFile))
            {
                Console.WriteLine($"ERROR: No solution file was specified or configured.");
                OutputHelp();

                Log.Error($"No solution file was specified.");
                Environment.Exit(1);
            }

            var path = Path.GetFullPath(solutionFile);
            var pathRoot = Path.GetDirectoryName(path);

            var rtn = ParseTdsProjects(solutionFile, pathRoot, helixModule);

            if (rtn)
            {
                Console.WriteLine($"Modules created! Go check Visual Studio.");
            }
        }

        static bool ParseTdsProjects(string solutionFile, string pathRoot, string helixModule)
        {
            foreach (string projLine in File.ReadLines(solutionFile))
            {
                if (!projLine.StartsWith("Project(\"{CAA73BB0-EF22-4D79-A57E-DF67B3BA9C80}\""))
                {
                    continue;
                }

                // It's a TDS project!
                var tdsProjFile = projLine.Replace("Project(\"{CAA73BB0-EF22-4D79-A57E-DF67B3BA9C80}\") = ", "");
                var tdsProjAr = tdsProjFile.Split(", ");
                var tdsFile = $"{pathRoot}\\" + tdsProjAr[1].Replace("\"", "");

                if (!File.Exists(tdsFile))
                {
                    continue;
                }

                var projectName = Path.GetFileName(tdsFile).Replace(".scproj", "");
                var modulePath = Path.GetFullPath(Path.Combine(tdsFile, @"..\..\..\"));
                DirectoryInfo moduleDir = new DirectoryInfo(tdsFile);

                // This will be the filename of the generated module
                var moduleFilename = modulePath + projectName + ".module.json";

                Log.Info($"=[ Parsing: {projectName} => {tdsFile}");
                Log.Info($"==[ Creating module file: {moduleFilename} ");

                // Create a rule list and an include list
                var rules = new List<ScsModuleModel.Rule>();
                var includes = new List<ScsModuleModel.Include>();

                // Parse TDS file
                using (var fileStream = File.Open(tdsFile, FileMode.Open))
                {
                    // Open the TDS project file, and XML Deserialize
                    var tdsProjectBase = Path.GetDirectoryName(tdsFile);
                    XmlSerializer serializer = new XmlSerializer(typeof(TdsProjectModel.Project));
                    var tdsProj = (TdsProjectModel.Project)serializer.Deserialize(fileStream);

                    // Clean some things up on start of new project
                    var lastSection = string.Empty;
                    var firstPath = string.Empty;
                    var lastRule = string.Empty;
                    var filterItem = string.Empty;
                    var includePath = string.Empty;
                    var includeBasePath = string.Empty;

                    var addRules = new List<ScsModuleModel.Rule>();
                    var addIncludes = new List<ScsModuleModel.Include>();

                    List<string> tdsItems = new List<string>();
                    string tdsDatabase = "master";

                    // Now to parse the list of TDS items in the TDS Project and load in the file
                    foreach (var item in tdsProj.ItemGroup)
                    {
                        var itemFile = tdsProjectBase + "\\" + item.Include.ToString();

                        // it doesn't handle $(?) proper
                        if (itemFile.Contains("%2524"))
                        {
                            itemFile = itemFile.Replace("%2524", "%24");
                        }

                        foreach (string line in File.ReadLines(itemFile))
                        {
                            if (line.StartsWith("database:"))
                            {
                                var scDatabase = line[10..];
                                if (!string.IsNullOrEmpty(scDatabase))
                                    tdsDatabase = scDatabase;
                            }
                            if (line.StartsWith("path:"))
                            {
                                var scPath = line[6..];
                                if (!string.IsNullOrEmpty(scPath))
                                    tdsItems.Add(scPath);
                            }
                        }
                    }

                    var firstRuleAdded = false;

                    //now create the module

                    foreach (var scPath in tdsItems)
                    {
                        if (scPath.Contains($"Feature/{helixModule}", StringComparison.InvariantCultureIgnoreCase)
                            || scPath.Contains($"Foundation/{helixModule}", StringComparison.InvariantCultureIgnoreCase)
                            || scPath.Contains($"Project/{helixModule}", StringComparison.InvariantCultureIgnoreCase)
                            || scPath.Contains($"Content/{helixModule}", StringComparison.InvariantCultureIgnoreCase)
                            || scPath.Contains($"client/Your Apps", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var newSection = false;
                            var scSection = GetSitecoreSection(scPath);

                            if (string.IsNullOrEmpty(lastSection))
                                lastSection = scSection;

                            if (scSection != lastSection)
                                newSection = true;

                            if (string.IsNullOrEmpty(includePath))
                                includeBasePath = includePath;

                            if (!scPath.Contains(firstPath) && firstRuleAdded)
                                firstRuleAdded = false;

                            // This handles most the helix type modules
                            if ((scPath.Contains($"Feature/{helixModule}/", StringComparison.InvariantCultureIgnoreCase)
                                || scPath.Contains($"Foundation/{helixModule}/", StringComparison.InvariantCultureIgnoreCase)
                                || scPath.Contains($"Project/{helixModule}/", StringComparison.InvariantCultureIgnoreCase)
                                || scPath.Contains($"Content/{helixModule}/", StringComparison.InvariantCultureIgnoreCase)
                                || scPath.Contains($"client/Your Apps/", StringComparison.InvariantCultureIgnoreCase))
                                && !firstRuleAdded)
                            {
                                // first added
                                firstPath = scPath;
                                includePath = scPath.Substring(0, firstPath.LastIndexOf('/'));
                                filterItem = scPath.Substring(firstPath.LastIndexOf('/'));

                                // create include here
                                Log.Info($"===[Rule]: {filterItem} / {scPath}");
                                var rule = new ScsModuleModel.Rule()
                                {
                                    Path = filterItem,
                                    AllowedPushOperations = "createAndUpdate",
                                    Scope = "itemAndDescendants"
                                };
                                addRules.Add(rule);
                                firstRuleAdded = true;
                            }

                            // Create the include
                            if (newSection)
                            {
                                if (!string.IsNullOrEmpty(includePath))
                                {
                                    Log.Info($"==[Include]: {scSection} / {includePath}");
                                    var finalRule = new ScsModuleModel.Rule()
                                    {
                                        Path = "*",
                                        Scope = "ignored",
                                    };
                                    addRules.Add(finalRule);

                                    var include = new ScsModuleModel.Include()
                                    {
                                        AllowedPushOperations = "createAndUpdate",
                                        Name = lastSection,
                                        Path = includePath,
                                        Scope = "itemAndDescendants",
                                        Database = tdsDatabase,
                                        Rules = new List<ScsModuleModel.Rule>(addRules),
                                    };

                                    if (!string.IsNullOrEmpty(Config["maxRelativePathLength"]))
                                    {
                                        include.MaxRelativePathLength = Config["maxRelativePathLength"];
                                    }

                                    addIncludes.Add(include);

                                    // reset
                                    addRules.Clear();
                                    firstPath = string.Empty;
                                    includePath = string.Empty;
                                    filterItem = string.Empty;
                                    lastSection = scSection;
                                }
                                else
                                {
                                    Log.Warn($"==[Include]: Missing: {scSection} / {scPath}");
                                }

                            }

                        }
                        // If it's a system folder
                        else if (scPath.StartsWith($"/sitecore/system/", StringComparison.InvariantCultureIgnoreCase))
                        {
                            // We need to be careful with these ones
                            // TODO: Maybe support this, idk
                        }
                        else
                        {
                            Log.Warn($"Unable to find a place for {scPath}");
                        }
                    }

                    var items = new ScsModuleModel.Items()
                    {
                        Includes = addIncludes
                    };

                    if (!string.IsNullOrEmpty(Config["serialisationFolder"]))
                    {
                        items.Path = Config["serialisationFolder"];
                    }

                    var scsModule = new ScsModuleModel.Root()
                    {
                        Namespace = projectName,
                        Description = projectName,
                        Items = items,
                    };

                    // Create that module file yo
                    string json = JsonConvert.SerializeObject(scsModule, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(moduleFilename, json);

                    Log.Info($"Module written for {moduleFilename}");
                }
            }

            return true;
        }

        static void OutputHelp()
        {
            Console.WriteLine($"  .___..__  __.    _,      __. __  __.");
            Console.WriteLine($"    |  |  \\(__    '_)     (__ /  `(__ ");
            Console.WriteLine($"    |  |__/.__) * / _.  * .__)\\__..__)");
            Console.WriteLine($"===================================[tds2scs]=");
            Console.WriteLine("\n");
            Console.WriteLine("Usage: ");
            Console.WriteLine("> tds2scs <fullpathofsolution>");
            Console.WriteLine("\n");
        }

        /// <summary>
        /// Returns the 'section' in Sitecore, eg templates or media library
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string GetSitecoreSection(string path)
        {
            // if it's one of the ones below, then do it

            if (path.StartsWith("/sitecore/Layout/Layouts", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Layouts";
            }

            if (path.StartsWith("/sitecore/Layout/Renderings", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Renderings";
            }

            if (path.StartsWith("/sitecore/Layout/Placeholder Settings", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Placeholders";
            }

            if (path.StartsWith("/sitecore/Media Library", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Media Library";
            }

            if (path.StartsWith("/sitecore/system", StringComparison.InvariantCultureIgnoreCase))
            {
                return "System";
            }

            if (path.StartsWith("/sitecore/client/Your Apps", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Your Apps";
            }

            return path.Split('/')[2];
        }

        #region Configuration 

        /// <summary>
        /// Application specific configuration, including log4net config
        /// </summary>
        /// <returns></returns>
        public static IConfigurationRoot GetAppSettings()
        {
            // get log4net config
            // ref: https://jakubwajs.wordpress.com/2019/11/28/logging-with-log4net-in-net-core-3-0-console-app/
            var logRepository = LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly());
            log4net.Config.XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            // get config settings from appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            return config;
        }

        public static void DumpObj(object obj)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented)); // your logger
        }

        #endregion
    }
}