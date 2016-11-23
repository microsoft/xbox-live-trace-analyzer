using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace XboxLiveTrace
{
    internal class ServiceCallData
    {
        public class PerConsoleData
        {
            public Dictionary<String, LinkedList<ServiceCallItem>> m_servicesHistory = new Dictionary<string, LinkedList<ServiceCallItem>>();
            public Dictionary<String, ServiceCallStats> m_servicesStats = new Dictionary<string, ServiceCallStats>();
        }

        public static bool m_allEndpoints = false; 

        public Dictionary<String, PerConsoleData> m_perConsoleData = new Dictionary<string, PerConsoleData>();
        public Dictionary<String, Tuple<String, String>> m_endpointToService;

        public ServiceCallData(bool allEndpoints)
        {
            m_allEndpoints = allEndpoints;
        }

        public void DeserializeJson(String input)
        {
            String data = File.ReadAllText(input);
            data = "{\"Data\":" + data + "}";
            XmlDocument doc = JsonConvert.DeserializeXmlNode(data, "Root");

            var consoleData = new PerConsoleData();
            m_perConsoleData.Add("Console", consoleData);

            //gather data from every endpoint
            foreach(XmlNode endpointNode in doc.FirstChild.ChildNodes)
            {
                String endpointName = endpointNode.FirstChild.InnerText;

                for (int j = 1; j < endpointNode.ChildNodes.Count; ++j)
                {
                    XmlNode itemNode = endpointNode.ChildNodes[j];
                    ServiceCallItem item = ServiceCallItem.FromJson(itemNode);
                    item.m_host = endpointName;
                    if (!consoleData.m_servicesHistory.ContainsKey(item.m_host))
                    {
                        consoleData.m_servicesHistory.Add(item.m_host, new LinkedList<ServiceCallItem>());
                    }
                    consoleData.m_servicesHistory[item.m_host].AddLast(item);
                }
            }
            GatherStats(consoleData);
        }

        public void DeserializeCSV(String input)
        {
            StreamReader data = new StreamReader(input);
            String version = data.ReadLine();
            if (version == Constants.Version1509 ||
                version == Constants.Version1510)
            {
                // Skip over the headers
                data.ReadLine();

                var consoleData = new PerConsoleData();
                m_perConsoleData.Add("Console", consoleData);

                while (!data.EndOfStream)
                {
                    String row = data.ReadLine();
                    ServiceCallItem item = ServiceCallItem.FromCSV1509(row);
                    item.m_logVersion = version;
                    if (item != null)
                    {
                        if (!consoleData.m_servicesHistory.ContainsKey(item.m_host))
                        {
                            consoleData.m_servicesHistory.Add(item.m_host, new LinkedList<ServiceCallItem>());
                        }
                        consoleData.m_servicesHistory[item.m_host].AddLast(item);
                    }
                }
                GatherStats(consoleData);
            }
        }

        public void DeserializeFiddlerTrace(String filePath, String customAgent)
        {
            // Open the SAZ
            var archive = System.IO.Compression.ZipFile.Open(filePath, ZipArchiveMode.Read);

            // Group the archive entries by frame number
            var result = from e in archive.Entries
                         where e.Name.Contains("_c.txt") || e.Name.Contains("_s.txt") || e.Name.Contains("_m.xml")
                         group e by Utils.GetFrameNumber(e.Name) into g
                         select new { Frame = g.Key, Data = g };

            List<ServiceCallItem> frameData = new List<ServiceCallItem>();

            // Process data per frame
            foreach (var group in result)
            {
                // Grab the individual files
                ZipArchiveEntry cFileArchive = group.Data.ElementAt(0);
                ZipArchiveEntry mFileArchive = group.Data.ElementAt(1);
                ZipArchiveEntry sFileArchive = group.Data.ElementAt(2);

                ServiceCallItem frame = null;

                frame = ServiceCallItem.FromFiddlerFrame((UInt32)group.Frame, cFileArchive, mFileArchive, sFileArchive);

                // If this is not an Xbox Service Endpoint that we are checking, then move along.
                if (frame == null || (!Utils.IsAnalyzedService(frame, customAgent) && m_allEndpoints == false))
                {
                    continue;
                }

                frameData.Add(frame);
            }

            var consoleGroups = from f in frameData
                                group f by f.m_consoleIP;

            foreach (var consoleFrames in consoleGroups.Where(g => g.Key != String.Empty))
            {
                var consoleData = new PerConsoleData();

                var xboxServiceFrames = consoleFrames.GroupBy(f => f.m_host)
                                                     .Select(group => new { Host = group.Key, History = group.AsEnumerable() });

                consoleData.m_servicesHistory = xboxServiceFrames.ToDictionary(g => g.Host, g => new LinkedList<ServiceCallItem>(g.History.OrderBy(call => call.m_reqTimeUTC)));

                m_perConsoleData.Add(consoleFrames.Key, consoleData);

                GatherStats(consoleData);
            }
        }

        private void GatherStats(PerConsoleData data)
        {
            foreach(String endpoint in data.m_servicesHistory.Keys)
            {
                data.m_servicesStats[endpoint] = new ServiceCallStats();
                ServiceCallStats stats = data.m_servicesStats[endpoint];
                
                //create new Request Body Hash Count Map
                stats.m_reqBodyHashCountMap = new Dictionary<UInt64 , UInt32 >();

                //Calculate 1st-order stats
                foreach (ServiceCallItem item in data.m_servicesHistory[endpoint])
                {
                    GatherFirstOrderStats(item, stats);
                }

                // Calculate 2nd-order stats (standard deviation)
                stats.m_numCalls = 0;
                stats.m_lastReqTimeUTC = 0;

                foreach (ServiceCallItem item in data.m_servicesHistory[endpoint])
                {
                    GatherSecondOrderStats(item, stats);
                }
            }
                
        }

        private void GatherFirstOrderStats(ServiceCallItem item, ServiceCallStats stats)
        {
            // Ignore shoulder taps
            if (item.m_isShoulderTap)
            {
                return;
            }

            UInt64 n = stats.m_numCalls;

            UInt64 avg = stats.m_avgElapsedCallTimeMs;
            avg = n * avg + item.m_elapsedCallTimeMs;
            avg /= (n + 1);
            
            //update values
            stats.m_avgElapsedCallTimeMs = avg;

            //track skipped calls
            if (item.m_reqTimeUTC < stats.m_lastReqTimeUTC)
            {
                ++stats.m_numSkippedCalls;
            }

            //m_avgTimeBetweenReqsMs
            if (stats.m_lastReqTimeUTC != 0 && item.m_reqTimeUTC >= stats.m_lastReqTimeUTC)
            {
                UInt64 avgTime = stats.m_avgTimeBetweenReqsMs;
                avgTime = n * avgTime + (item.m_reqTimeUTC - stats.m_lastReqTimeUTC) / TimeSpan.TicksPerMillisecond;
                avgTime /= (n + 1);
                stats.m_avgTimeBetweenReqsMs = avgTime;
            }

            //update last call time
            stats.m_lastReqTimeUTC = item.m_reqTimeUTC;

            //increment num calls for next time
            ++stats.m_numCalls;

            // Update m_maxElapsedCallTimeMs if applicable
            if (item.m_elapsedCallTimeMs > stats.m_maxElapsedCallTimeMs)
            {
                stats.m_maxElapsedCallTimeMs = item.m_elapsedCallTimeMs;
            }

            // m_reqBodyHashCountMap
            if (!stats.m_reqBodyHashCountMap.ContainsKey(item.m_reqBodyHash))
            {
                stats.m_reqBodyHashCountMap.Add(item.m_reqBodyHash, 1);
            }
            else
            {
                ++stats.m_reqBodyHashCountMap[item.m_reqBodyHash];
            }
        }

        private void GatherSecondOrderStats(ServiceCallItem item, ServiceCallStats stats)
        {
            // Ignore shoulder taps
            if (item.m_isShoulderTap)
            {
                return;
            }

            //
            // Calculate variance
            // 
            // Var[n+1] = ( n * Var[n] + ( x[n+1] - Avg ) ^ 2 ) / ( n + 1)
            //
            UInt64 avg = stats.m_avgElapsedCallTimeMs;
            UInt64 n = stats.m_numCalls;

            // m_varElapsedCallTimeMs
            UInt64 var = stats.m_varElapsedCallTimeMs;
            UInt64 dev = item.m_elapsedCallTimeMs - avg;
            var = n * var + dev * dev;
            var /= (n+1);

            //m_varTimeBetweenReqsMs
            if(stats.m_lastReqTimeUTC != 0 && item.m_reqTimeUTC >= stats.m_lastReqTimeUTC)
            {
                UInt64 localVar = stats.m_varTimeBetweenReqsMs;
                UInt64 localDev = (item.m_reqTimeUTC - stats.m_lastReqTimeUTC) / TimeSpan.TicksPerMillisecond - avg;
                localVar = n * localVar + localDev * localDev;
                localVar /= (n + 1);
                //update values
                stats.m_varTimeBetweenReqsMs = localVar;
            }

            stats.m_lastReqTimeUTC = item.m_reqTimeUTC;

            // increment m_numCalls for next time
            ++n;

            //update values
            stats.m_numCalls = n;
            stats.m_varElapsedCallTimeMs = var;


        }

        private void CalculateEntropy(String endpoint, ServiceCallStats stats)
        {
            //for(int i = 0; i < (int)ServiceCallStats.CallType.CallType_Count; ++i)
            {
                if(stats.m_numCalls == 0)
                {
                    return;
                }

                double entropy = 0.0f;

                foreach (UInt32 it in stats.m_reqBodyHashCountMap.Values)
                {
                    double p = ((double)it / (double)stats.m_numCalls);
                    entropy += p * Math.Log( 1 / p ) / Math.Log(2);
                }

                stats.m_callEntropy = entropy;

            }
        }
    }
}
