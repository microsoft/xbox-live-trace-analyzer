// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

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

            foreach (var endpoint in consoleData.m_servicesHistory)
            {
                consoleData.m_servicesStats.Add(endpoint.Key, new ServiceCallStats(endpoint.Value));
            }
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

                foreach (var endpoint in consoleData.m_servicesHistory)
                {
                    consoleData.m_servicesStats.Add(endpoint.Key, new ServiceCallStats(endpoint.Value));
                }
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

                frame = ServiceCallItem.FromFiddlerFrame((UInt32)group.Frame, cFileArchive, mFileArchive, sFileArchive, o => m_allEndpoints || Utils.IsAnalyzedService(o, customAgent));

                // If this is not an Xbox Service Endpoint that we are checking, then move along.
                if (frame == null)
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

                // Xbox telemetry endpoint
                if(consoleData.m_servicesHistory.ContainsKey("data-vef.xboxlive.com"))
                {
                    ConvertCS1ToEvent(consoleData.m_servicesHistory);
                }

                // Windows Telemetry endpoint
                if(consoleData.m_servicesHistory.Any(k => k.Key.Contains(".data.microsoft.com")))
                {
                    ConvertCS2ToEvent(consoleData.m_servicesHistory);
                }

                // clear empty items
                consoleData.m_servicesHistory = consoleData.m_servicesHistory.Where( o => o.Value.Count > 0).ToDictionary(x => x.Key, x => x.Value);

                foreach (var endpoint in consoleData.m_servicesHistory)
                {
                    consoleData.m_servicesStats.Add(endpoint.Key, new ServiceCallStats(endpoint.Value));
                }

                m_perConsoleData.Add(consoleFrames.Key, consoleData);
            }
        }

        private void ConvertCS2ToEvent(Dictionary<string, LinkedList<ServiceCallItem>> servicesHistory)
        {
            var eventNameMatch1 = new Regex("Microsoft.XboxLive.T[a-zA-Z0-9]{8}.");
            var eventNameMatch2 = "Microsoft.Xbox.XceBridge";

            var events = servicesHistory.Where(k => k.Key.Contains(".data.microsoft.com")).ToList();
            foreach (var endpoint in events)
            {
                servicesHistory.Remove(endpoint.Key);
            }
            LinkedList<ServiceCallItem> inGameEvents = null;
            if (servicesHistory.ContainsKey("inGameEvents"))
            {
                inGameEvents = servicesHistory["inGameEvents"];
            }
            else
            {
                inGameEvents = new LinkedList<ServiceCallItem>();
                servicesHistory.Add("inGameEvents", inGameEvents);
            }

            foreach (var eventCall in events.SelectMany(e => e.Value))
            {
                var requestBody = eventCall.m_reqBody;

                // If there's nothing in the request body, then there was an error with the event and we can't parse it.
                if(string.IsNullOrEmpty(requestBody))
                {
                    continue;
                }

                var eventArray = requestBody.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                foreach (var eventLine in eventArray)
                {
                    JObject requestBodyJson;

                    try
                    {
                        requestBodyJson = JObject.Parse(eventLine);
                    }
                    catch
                    {
                        continue;
                    }

                    var eventName = requestBodyJson["name"].ToString();

                    if (eventNameMatch1.IsMatch(eventName) || eventNameMatch2.StartsWith(eventName))
                    {
                        var serviceCall = eventCall.Copy();
                        var eventNameParts = eventName.Split('.');


                        serviceCall.m_host = "inGameEvents";
                        serviceCall.m_eventName = eventNameParts.Last();
                        serviceCall.m_reqTimeUTC =
                            (UInt64) DateTime.Parse(requestBodyJson["time"].ToString()).ToFileTimeUtc();
                        serviceCall.m_reqBody = String.Empty;

                        var data = requestBodyJson.GetValue("data") as JObject;
                        if (data != null)
                        {
                            var baseData = data.GetValue("baseData") as JObject;

                            if (baseData != null)
                            {
                                var measurements = baseData["measurements"];
                                serviceCall.m_measurements =
                                    measurements != null ? measurements.ToString() : String.Empty;

                                var properties = baseData["properties"];
                                if (serviceCall.m_eventName.Contains("MultiplayerRoundStart") ||
                                    serviceCall.m_eventName.Contains("MultiplayerRoundEnd"))
                                {
                                    serviceCall.m_playerSessionId = baseData["playerSessionId"].ToString();
                                    serviceCall.m_multiplayerCorrelationId =
                                        properties["MultiplayerCorrelationId"].ToString();
                                }
                            }
                        }

                        inGameEvents.AddLast(serviceCall);
                    }
                }

            }

        }

        private void ConvertCS1ToEvent(Dictionary<string, LinkedList<ServiceCallItem>> servicesHistory)
        {
            var events = servicesHistory["data-vef.xboxlive.com"];
            servicesHistory.Remove("data-vef.xboxlive.com");

            LinkedList<ServiceCallItem> inGameEvents = null;
            if (servicesHistory.ContainsKey("inGameEvents"))
            {
                inGameEvents = servicesHistory["inGameEvents"];
            }
            else
            {
                inGameEvents = new LinkedList<ServiceCallItem>();
                servicesHistory.Add("inGameEvents", inGameEvents);
            }

            // Event Name starts with a string in the form of {Publisher}_{TitleId}
            Regex eventNameMatch = new Regex("[a-zA-z]{4}_[a-zA-Z0-9]{8}");

            foreach(var eventCall in events)
            {
                var requestBody = eventCall.m_reqBody;

                var eventArray = requestBody.Split(Environment.NewLine.ToCharArray());

                foreach(var eventLine in eventArray)
                {
                    var fields = eventLine.Split('|');

                    if(fields.Length < 12)
                    {
                        // This event is not valid as it is missing fields
                        continue;
                    }

                    // The name field is in the form of {Publisher}_{TitleId}.{EventName}
                    var eventNameParts = fields[1].Split('.');

                    if(eventNameParts.Length > 1 && eventNameMatch.IsMatch(eventNameParts[0]))
                    {
                        ServiceCallItem splitEvent = eventCall.Copy();

                        splitEvent.m_host = "inGameEvents";
                        splitEvent.m_eventName = eventNameParts[1];
                        splitEvent.m_reqTimeUTC = (UInt64)DateTime.Parse(fields[2]).ToFileTimeUtc();
                        splitEvent.m_reqBody = String.Empty;
                        splitEvent.m_dimensions = CS1PartBC(fields);
                        splitEvent.m_isInGameEvent = true;
                        
                        if(splitEvent.m_eventName.Contains("MultiplayerRoundStart") || splitEvent.m_eventName.Contains("MultiplayerRoundEnd"))
                        {
                            splitEvent.m_playerSessionId = fields[15];
                            splitEvent.m_multiplayerCorrelationId = fields[16];
                        }

                        inGameEvents.AddLast(splitEvent);
                    }
                }
            }
        }

        private static string CS1PartBC(string[] fields)
        {
            string result = "";

            for(int i = 1; i < fields.Length; ++i)
            {
                result += fields[i] + "|";
            }

            return result;
        }
    }
}
