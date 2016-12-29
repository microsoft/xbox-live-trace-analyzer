using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Reflection;

namespace XboxLiveTrace
{
    public class TraceAnalyzer
    {
        List<IReport> m_reports = new List<IReport>();
        List<IRule> m_rules = new List<IRule>();
        UriToMethodConverter m_converter = new UriToMethodConverter();
        ServiceCallData m_data;
        RulesEngine m_rulesEngine;

        bool m_online;
        bool m_IsLatestBinary = true;
        bool m_IsLatestRules = true;
        String m_latestBinaryVersion = String.Empty;
        String m_latestRuleVersion = String.Empty;
        String m_ruleVersion = String.Empty;

        public String OutputDirectory { get; set; }
        public IEnumerable<String> ConsoleList
        {
            get
            {
                return m_data.m_perConsoleData.Keys;
            }
        }

        public String CustomUserAgent { get; set; }

        public TraceAnalyzer(bool isInternal, bool allEndpoints, bool online)
        {
            m_data = new ServiceCallData(allEndpoints);
            m_rulesEngine = new RulesEngine(isInternal);
            m_online = online;

            if (online)
            {
                GetLatestVersionNumbers();
            }
            else
            {
                m_latestBinaryVersion = VersionInfo.Version;
            }
        }

        public void LoadData(String dataFilePath)
        {
            String extension = Path.GetExtension(dataFilePath);

            switch (extension)
            {
                case ".json": // 1.0 logs
                    m_data.DeserializeJson(dataFilePath);
                    break;
                case ".csv": // 2.0 logs
                case ".txt":
                    m_data.DeserializeCSV(dataFilePath);
                    break;
                case ".saz": // Fiddler Trace
                    m_data.DeserializeFiddlerTrace(dataFilePath, CustomUserAgent);
                    break;
                default:
                    throw new ArgumentException("Data file \"" + dataFilePath + "\" is not a supported file type.");
            }
        }

        public bool IsDataContainsEndpoint(string endpoint)
        {
            foreach(var console in m_data.m_perConsoleData.Values)
            {
                if(console.m_servicesHistory.ContainsKey(endpoint))
                {
                    return true;
                }
            }
            return false;
        }

        public void LoadURIMap(StreamReader mapFile)
        {
            m_converter.LoadMap(mapFile);
        }

        public void LoadDefaultURIMap()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var embeddedRules = assembly.GetManifestResourceStream("XboxLiveTrace.XboxLiveTraceAnalyzer.APIMap.csv"))
            {
                using (var file = new StreamReader(embeddedRules))
                {
                    LoadURIMap(file);
                }
            }
        }

        public void LoadRules(TextReader rules)
        {
            using (JsonTextReader reader = new JsonTextReader(rules))
            {
                
                JObject jsonObject = JObject.Load(reader);
                JToken parseToken;

                // Read the version string
                if (jsonObject.TryGetValue("Version", out parseToken) == true)
                {
                    m_ruleVersion = parseToken.ToString();
                }

                // Parse the rules from the data
                if (jsonObject.TryGetValue("Rules", out parseToken) == true)
                {
                    var ruleParameters = parseToken as JArray;

                    // Loop over each rule in the array
                    foreach (var ruleDef in ruleParameters)
                    {
                        var ruleName = ruleDef["Type"].ToString();
                        // Using C# reflection look up the rule's type
                        // This way we can just make the rules and not worry about adding it to the RuleEngine
                        // This does require that rules are named in this format: {RuleName}Rule. ie- CallFrequency/Rule/.
                        Type ruleType = Type.GetType("XboxLiveTrace." + ruleName + "Rule");

                        // If the rule isn't a built in rule, check for a custom rule.
                        if(ruleType == null)
                        {
                            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                            foreach (var assembly in assemblies.Where(a => a.GlobalAssemblyCache == false))
                            {
                                foreach(var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(IRule))))
                                {
                                    if(type.Name.EndsWith(ruleName))
                                    {
                                        ruleType = type;
                                        break;
                                    }
                                }

                                if(ruleType != null)
                                {
                                    break;
                                }
                            }
                        }

                        // Make sure the type was value
                        if (ruleDef["Type"] != null && ruleType != null)
                        {
                            // Create the rule and cast to Rule
                            IRule rule = Activator.CreateInstance(ruleType) as IRule;

                            // Fill in the data
                            if (ruleDef["Name"] != null)
                            {
                                rule.Name = ruleDef["Name"].ToString();
                            }
                            if (ruleDef["Endpoint"] != null)
                            {
                                rule.Endpoint = ruleDef["Endpoint"].ToString();
                            }
                            if (ruleDef["Properties"] != null)
                            {
                                rule.DeserializeJson(ruleDef["Properties"] as JObject);
                            }

                            m_rules.Add(rule);
                        }
                        else
                        {
                            throw new Exception("Invalid rule type " + ruleDef["Name"] + " in rule definition file.");
                        }

                    }
                }
            }
        }

        public void LoadRules(String rulesFilePath)
        {
            using (System.IO.StreamReader rules = new StreamReader(rulesFilePath))
            {
                LoadRules(rules);
            }
        }

        public void AddRule(IRule r)
        {
            m_rules.Add(r);
        }

        public void SaveRules(String rulesFilePath)
        {
            m_rulesEngine.SerializeJson(rulesFilePath);
        }

        public void AddReport(IReport report)
        {
            m_reports.Add(report);
        }

        public void AddReport(string reportType, string outputFile)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                Type[] types = null;
                try
                {
                    types = assembly.GetTypes();
                }
                catch(ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                foreach (var type in types.Where(t => t != null && typeof(IReport).IsAssignableFrom(t)))
                {
                    if (type.Name.EndsWith(reportType))
                    {
                        IReport report = Activator.CreateInstance(type) as IReport;
                        m_reports.Add(report);
                        return;
                    }
                }
            }
        }

        public void LoadDefaultRules()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var embeddedRules = assembly.GetManifestResourceStream("XboxLiveTrace.XboxLiveTraceAnalyzer.Rules.json"))
            {
                using (var file = new StreamReader(embeddedRules))
                {
                    LoadRules(file);
                }
            }
        }

        public void Run()
        {
            if (m_data.m_perConsoleData.Count == 1)
            {
                var console = m_data.m_perConsoleData.First();

                m_rulesEngine.ClearAllRules();
                m_rulesEngine.AddRules(m_rules);

                m_rulesEngine.RunRulesOnData(console.Key, console.Value);

                Parallel.ForEach(m_reports, report => report.RunReport(OutputDirectory, m_rulesEngine.GetResults(console.Key), m_data.m_endpointToService, m_IsLatestBinary, m_latestBinaryVersion));
            }
            else
            {
                foreach (var console in m_data.m_perConsoleData)
                {
                    m_rulesEngine.ClearAllRules();
                    m_rulesEngine.AddRules(m_rules);

                    m_rulesEngine.RunRulesOnData(console.Key, console.Value);

                    String consolePath = Path.Combine(OutputDirectory, console.Key);

                    if (!Directory.Exists(consolePath))
                    {
                        Directory.CreateDirectory(consolePath);
                    }

                    Parallel.ForEach(m_reports, report => report.RunReport(consolePath, m_rulesEngine.GetResults(console.Key), m_data.m_endpointToService, m_IsLatestBinary, m_latestBinaryVersion));
                }
            }
        }

        public void RunUriConverterOnData()
        {
            foreach (var console in m_data.m_perConsoleData.Values)
            {
                foreach (var endpointData in console.m_servicesHistory)
                {
                    var service = m_converter.GetService(endpointData.Key);

                    if (service != null)
                    {
                        foreach (var call in endpointData.Value)
                        {
                            call.m_xsapiMethods = m_converter.GetMethod(call.m_uri, call.m_isGet);
                        }
                    }
                }
            }

            m_data.m_endpointToService = m_converter.GetServices();
            
        }

        public void GetLatestVersionNumbers()
        {        
            m_latestBinaryVersion = VersionInfo.Version;

            var localVersion = Version.Parse(VersionInfo.Version);
            var currentVersion = Version.Parse(m_latestBinaryVersion);

            m_IsLatestBinary = localVersion >= currentVersion;
        }

        public void LoadPlugins(String pluginDir)
        {
            var pluginDirectory = System.IO.Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), pluginDir);
            if (Directory.Exists(pluginDirectory))
            {
                foreach (var plugin in Directory.EnumerateFiles(pluginDirectory, "*.dll"))
                {
                    var assembly = Assembly.LoadFrom(plugin);

                    //AppDomain.CurrentDomain.;
                }
            }
        }

        public bool CheckRulesVersion(String rulesVersion)
        {
            m_IsLatestRules = (m_latestRuleVersion == rulesVersion);
            return m_IsLatestRules;
        }

        public static String CurrentVersion
        {
            get { return VersionInfo.Version; }
        }
    }
}
