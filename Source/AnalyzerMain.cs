// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Globalization;
using XboxLiveTrace;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace XboxLiveTrace
{
    class AnalyzerMain
    {
        public static char[] m_delimeters = new char[] { ';' };

        public static String m_dataFilePath = String.Empty;
        public static String m_rulesFilePath = String.Empty;
        public static String m_outputDirectory = String.Empty;
        public static String m_customUserAgent = String.Empty;

        public static bool m_graphImages = true;
        public static bool m_allEndpoints = false;
        public static bool m_isInternal = false;
        public static bool m_forceVersionCheck = false;
        public static bool m_jsonOnly = false;
        public static bool m_defaults = true;
        public static List<String> m_reports = new List<String>();

        static void OutputHelp()
        {
            Console.WriteLine("XBLTraceAnalyzer.exe. Version {0}.", TraceAnalyzer.CurrentVersion);
            Console.WriteLine("Analyzes title Xbox Live calling patterns given correctly formatted data.\n");

            Console.WriteLine("XBLTraceAnalyzer.exe [-data ...] [-outputdir ...] [-reports defaults,type1,type2,etc.] [-customUserAgent agent]\n");

            Console.WriteLine("-data:\t\tSpecifies the path to the data file in JSON, CSV, or SAZ format.");
            Console.WriteLine("-rules:\tLoads a custom rules definition.");
            Console.WriteLine("-outputdir:\tSpecifies the directory to print reports to. Default: '.\\'");
            Console.WriteLine("-json:\tOutputs only the json report, not the full html report.");
            Console.WriteLine("-reports:\tA comma separated list of the types of reports you want generated. Use \"default\" for the standard LTA reports.");
            Console.WriteLine("-customUserAgent:\tSpecify the User Agent used when making call to your own services to enable analysis on non-XSAPI calls.");
            Console.WriteLine("-h[elp]:\tShow this help text");
        }

        static bool ParseArguments(string[] args)
        {
            if (args.Contains("-h") || args.Contains("-help"))
            {
                return false;
            }

            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] == "-data")
                {
                    // The next arg is the file location
                    ++i;
                    if (i >= args.Length)
                    {
                        // Error if there are no more arguments
                        Console.WriteLine("Parameter \"-data\" requires a file path");
                        return false;
                    }
                    else if (File.Exists(args[i]) == false)
                    {
                        // Don't just run the analyzer with the default data if the data parameter was bad
                        Console.WriteLine("Error: {0} is not a valid data file.", args[i]);
                        return false;
                    }
                    else
                    {
                        m_dataFilePath = args[i];
                    }

                }
                else if (args[i] == "-rules")
                {
                    // The next arg is the rules location
                    ++i;
                    if (i >= args.Length)
                    {
                        // Error if there are no more arguments
                        Console.WriteLine("Parameter \"-rules\" requires a file path");
                        return false;
                    }
                    else if (File.Exists(args[i]) == false)
                    {
                        Console.WriteLine("Warning: {0} is not a valid rules file. Using default rules.", args[i]);

                    }
                    else
                    {
                        m_rulesFilePath = args[i];
                    }
                }
                else if (args[i] == "-outputdir")
                {
                    // The next arg is where the report should be located
                    ++i;
                    if (i >= args.Length || args[i].StartsWith("-"))
                    {
                        // Error if there are no more arguments
                        Console.WriteLine("Parameter \"-outputdir\" requires a directory path");
                        return false;
                    }
                    else if (!Directory.Exists(args[i]))
                    {
                        // Create the report directory if it doesn't exist.
                        System.IO.Directory.CreateDirectory(args[i]);
                    }

                    m_outputDirectory = args[i];
                }
                else if (args[i] == "-allendpoints")
                {
                    m_allEndpoints = true;
                }
                else if (args[i] == "-internal")
                {
                    m_isInternal = true;
                }
                else if(args[i] == "-json")
                {
                    m_jsonOnly = true;
                }
                else if(args[i] == "-reports")
                {
                    ++i;

                    var reports = args[i].Split(',');

                    m_reports.AddRange(reports);

                    if(m_reports.Contains("default"))
                    {
                        m_reports.Remove("default");
                    }
                    else
                    {
                        m_defaults = false;
                    }
                }
                else if(args[i] == "-customUserAgent")
                {
                    ++i;
                    m_customUserAgent = args[i];
                }
                else
                {
                    Console.WriteLine("Invalid Parameter: " + args[i]);
                    return false;
                }
            }

            // Set defaults if these where not set by command line switches, and check files and directories exist

            if (string.IsNullOrEmpty(m_dataFilePath))
            {
                m_dataFilePath = Utils.GeneratePathToFile("data.csv");
                Console.WriteLine("No argument provided. Using data file: \"data.csv\"");
            }
            if (!File.Exists(m_dataFilePath))
            {
                Console.WriteLine("{0} - data missing - file not found.", m_dataFilePath);
                return false;
            }

            if (string.IsNullOrEmpty(m_outputDirectory))
            {
                m_outputDirectory = ".\\";
                Console.WriteLine("Writing reports to default location: \".\\\"");
            }
            if (!Directory.Exists(m_outputDirectory))
            {
                Directory.CreateDirectory(m_outputDirectory);
            }


            return true;
        }

        static public void Main(String[] args)
        {
            // If there is an error or the help command is passed
            if (ParseArguments(args) == false)
            {
                //Show the help message
                OutputHelp();
                return;
            }

            XboxLiveTrace.TraceAnalyzer analyzer = new TraceAnalyzer(m_isInternal, m_allEndpoints);

            try
            {
                analyzer.OutputDirectory = m_outputDirectory;
                analyzer.CustomUserAgent = m_customUserAgent;

                analyzer.LoadData(m_dataFilePath);

                if (String.IsNullOrEmpty(m_rulesFilePath) == false)
                {
                    analyzer.LoadRules(m_rulesFilePath);
                }

                analyzer.LoadDefaultRules();

                if (m_defaults)
                {
                    analyzer.LoadDefaultURIMap();
                    analyzer.RunUriConverterOnData();

                    analyzer.AddRule(new StatsRecorder { Endpoint = "*" });
                    analyzer.AddRule(new CallRecorder { Endpoint = "*" });
                    analyzer.AddRule(new XR049Rule { Endpoint = "userpresence.xboxlive.com" });
                    analyzer.AddReport(new PerEndpointJsonReport( m_jsonOnly));
                    analyzer.AddReport(new CallReport( m_jsonOnly));
                    analyzer.AddReport(new StatsReport( m_jsonOnly));
                    analyzer.AddReport(new CertWarningReport( m_jsonOnly));

                    if (!m_jsonOnly)
                    {
                        String tempZipPath = Path.Combine(m_outputDirectory, "html-report.zip");

                        var assembly = Assembly.GetExecutingAssembly();
                        using (var htmlReport = assembly.GetManifestResourceStream("XboxLiveTrace.html-report.zip"))
                        {
                            using (var outputFile = new System.IO.Compression.ZipArchive(htmlReport))
                            {
                                
                                foreach (var entry in outputFile.Entries.Where(e => e.Name.Contains('.')))
                                {
                                    bool create_subDir = analyzer.ConsoleList.Count() > 1;
                                    foreach (var console in analyzer.ConsoleList)
                                    {
                                        String path = m_outputDirectory;
                                        if (create_subDir)
                                        {
                                            path = Path.Combine(path, console);
                                        }

                                        if (!Directory.Exists(path))
                                        {
                                            Directory.CreateDirectory(path);
                                        }

                                        if (!Directory.Exists(Path.Combine(path, "css")))
                                            Directory.CreateDirectory(Path.Combine(path, "css"));
                                        if (!Directory.Exists(Path.Combine(path, "img")))
                                            Directory.CreateDirectory(Path.Combine(path, "img"));
                                        if (!Directory.Exists(Path.Combine(path, "js")))
                                            Directory.CreateDirectory(Path.Combine(path, "js"));

                                        using (var temp = entry.Open())
                                        {
                                            using (var reportFile = new FileStream(Path.Combine(path, entry.FullName), FileMode.Create))
                                            {
                                                temp.CopyTo(reportFile);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        System.IO.File.Delete(tempZipPath);
                    }
                }

                analyzer.LoadPlugins("plugins");

                foreach(var report in m_reports)
                {
                    analyzer.AddReport(report, m_outputDirectory);
                }

                analyzer.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                //Show the help message
                OutputHelp();
                return;
            }
        }
    }
}

