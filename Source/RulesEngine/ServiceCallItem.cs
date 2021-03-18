// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using System;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Net;
using System.Linq;

namespace XboxLiveTrace
{
    public class ServiceCallItem
    {
        private static UInt32 s_id = 0;
        public String m_logVersion;

        public String m_host;
        public String m_uri;
        public String m_xboxUserId;
        public String m_multiplayerCorrelationId;
        public String m_reqHeader;
        public String m_reqBody;
        public String m_rspHeader;
        public String m_rspBody;
        public String m_rspFullString;
        public String m_branch;
        public String m_sessionReferenceUriPath;
        public String m_eventName;
        public String m_playerSessionId;
        public String m_dimensions;
        public String m_measurements;
        public String m_breadCrumb;
        public Tuple<String,String, String> m_xsapiMethods;
        private String m_callDataFromJSON;

        public String m_consoleIP;

        public UInt16 m_version;

        public UInt32 m_id;
        public UInt32 m_httpStatusCode;

        public UInt64 m_reqBodyHash;
        public UInt64 m_elapsedCallTimeMs;
        public UInt64 m_reqTimeUTC;
        public UInt64 m_startTimeUTC;
        public UInt64 m_changeNumber;

        public bool m_isGet;
        public string m_method;
        public bool m_isShoulderTap;
        public bool m_isInGameEvent;

        public ServiceCallItem()
        {
            m_id = ++s_id;
            m_breadCrumb = Guid.NewGuid().ToString();
            m_consoleIP = String.Empty;
            Reset();
        }

        void Reset()
        {
            m_callDataFromJSON = String.Empty;
            m_version = 0;
            m_httpStatusCode = 0;
            m_reqBodyHash = 0;
            m_elapsedCallTimeMs = 0;
            m_reqTimeUTC = 0;
            m_startTimeUTC = 0;
            m_changeNumber = 0;

            m_isGet = false;
            m_isShoulderTap = false;
            m_isInGameEvent = false;
        }

        public ServiceCallItem Copy()
        {
            var copy = new ServiceCallItem();
            copy.m_logVersion = m_logVersion;
            copy.m_host = m_host;
            copy.m_uri = m_uri;
            copy.m_xboxUserId = m_xboxUserId;
            copy.m_multiplayerCorrelationId = m_multiplayerCorrelationId;
            copy.m_reqHeader = m_reqHeader;
            copy.m_reqBody = m_reqBody;
            copy.m_rspHeader = m_rspHeader;
            copy.m_rspBody = m_rspBody;
            copy.m_rspFullString = m_rspFullString;
            copy.m_branch = m_branch;
            copy.m_sessionReferenceUriPath = m_sessionReferenceUriPath;
            copy.m_eventName = m_eventName;
            copy.m_playerSessionId = m_playerSessionId;
            copy.m_dimensions = m_dimensions;
            copy.m_measurements = m_measurements;
            copy.m_breadCrumb = m_breadCrumb;
            copy.m_xsapiMethods = m_xsapiMethods;
            copy.m_callDataFromJSON = m_callDataFromJSON;
            copy.m_consoleIP = m_consoleIP;
            copy.m_version = m_version;
            copy.m_httpStatusCode = m_httpStatusCode;
            copy.m_reqBodyHash = m_reqBodyHash;
            copy.m_elapsedCallTimeMs = m_elapsedCallTimeMs;
            copy.m_reqTimeUTC = m_reqTimeUTC;
            copy.m_startTimeUTC = m_startTimeUTC;
            copy.m_changeNumber = m_changeNumber;
            copy.m_isGet = m_isGet;
            copy.m_method = m_method;
            copy.m_isShoulderTap = m_isShoulderTap;
            copy.m_isInGameEvent = m_isInGameEvent;

            return copy;
        }

        #region Factory Methods
        public static ServiceCallItem FromJson(XmlNode serviceCallNode)
        {
            ServiceCallItem item = new ServiceCallItem();

            foreach(XmlNode propertyNode in serviceCallNode.ChildNodes)
            {
                try
                {
                    //check for each service call property
                    if (propertyNode.Name == "Uri")
                    {
                        item.m_uri = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "Host")
                    {
                        item.m_host = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "XboxUserId")
                    {
                        item.m_xboxUserId = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "MultiplayerCorrelationId")
                    {
                        item.m_multiplayerCorrelationId = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "RequestHeaders")
                    {
                        item.m_reqHeader = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "RequestBody")
                    {
                        item.m_reqBody = propertyNode.InnerText;
                        item.m_reqBodyHash = (ulong)item.m_reqBody.GetHashCode();
                    }
                    else if (propertyNode.Name == "ResponseHeaders")
                    {
                        item.m_rspHeader = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "ResponseBody")
                    {
                        item.m_rspBody = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "FullResponse")
                    {
                        item.m_rspFullString = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "HttpStatusCode")
                    {
                        UInt32.TryParse(propertyNode.InnerText, out item.m_httpStatusCode);
                    }
                    else if (propertyNode.Name == "RequestBodyHashCode")
                    {
                        UInt64.TryParse(propertyNode.InnerText, out item.m_reqBodyHash);
                    }
                    else if (propertyNode.Name == "ElapsedCallTimeMs")
                    {
                        UInt64.TryParse(propertyNode.InnerText, out item.m_elapsedCallTimeMs);
                    }
                    else if (propertyNode.Name == "ReqTimeUTC")
                    {
                        double reqTimeUTC;
                        bool valid = double.TryParse(propertyNode.InnerText, out reqTimeUTC);
                        if(valid)
                        {
                            item.m_reqTimeUTC = (UInt64)BitConverter.DoubleToInt64Bits(reqTimeUTC);
                        }
                    }
                    else if (propertyNode.Name == "StartTimeUTC")
                    {
                        double startTimeUTC;
                        bool valid = double.TryParse(propertyNode.InnerText, out startTimeUTC);
                        if (valid)
                        {
                            item.m_startTimeUTC = (UInt64)BitConverter.DoubleToInt64Bits(startTimeUTC);
                        }
                    }
                    else if (propertyNode.Name == "IsGet")
                    {
                        bool.TryParse(propertyNode.InnerText, out item.m_isGet);
                    }
                    else if (propertyNode.Name == "Id")
                    {
                        UInt32.TryParse(propertyNode.InnerText, out item.m_id);
                    }
                    else if (propertyNode.Name == "IsShoulderTap")
                    {
                        bool.TryParse(propertyNode.InnerText, out item.m_isShoulderTap);
                    }
                    else if (propertyNode.Name == "ChangeNumber")
                    {
                        UInt64.TryParse(propertyNode.InnerText, out item.m_changeNumber);
                    }
                    else if (propertyNode.Name == "Branch")
                    {
                        item.m_branch = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "SessionReferenceUriPath")
                    {
                        item.m_sessionReferenceUriPath = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "IsInGameEvent")
                    {
                        bool.TryParse(propertyNode.InnerText, out item.m_isInGameEvent);
                    }
                    else if (propertyNode.Name == "EventName")
                    {
                        item.m_eventName = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "EventPlayerSessionId")
                    {
                        item.m_playerSessionId = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "EventVersion")
                    {
                        UInt16.TryParse(propertyNode.InnerText, out item.m_version);
                    }
                    else if (propertyNode.Name == "EventDimensionsData")
                    {
                        item.m_dimensions = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "EventMeasurementsData")
                    {
                        item.m_measurements = propertyNode.InnerText;
                    }
                    else if (propertyNode.Name == "BreadCrumb")
                    {
                        item.m_breadCrumb = propertyNode.InnerText;
                    }
                }
                catch (ArgumentException)
                {
                    
                }
            }

            return item;
        }

        public static ServiceCallItem FromCSV1509(String row)
        {
            ServiceCallItem item = new ServiceCallItem();

            String[] values = Utils.GetCSVValues(row);
            try
            {
                item.m_host = values[(UInt32)CSVValueIndex.Host];
                item.m_uri = values[(UInt32)CSVValueIndex.Uri];
                item.m_xboxUserId = values[(UInt32)CSVValueIndex.XboxUserId];
                item.m_multiplayerCorrelationId = values[(UInt32)CSVValueIndex.MultiplayerCorrelationId];
                item.m_reqHeader = values[(UInt32)CSVValueIndex.RequestHeader];
                item.m_reqBody = values[(UInt32)CSVValueIndex.RequestBody];
                item.m_reqBodyHash = (ulong)item.m_reqBody.GetHashCode();
                item.m_rspHeader = values[(UInt32)CSVValueIndex.ResponseHeader];
                item.m_rspBody = values[(UInt32)CSVValueIndex.ResponseBody];
                UInt32.TryParse(values[(UInt32)CSVValueIndex.HttpStatusCode], out item.m_httpStatusCode);
                UInt64.TryParse(values[(UInt32)CSVValueIndex.ElapsedCallTime], out item.m_elapsedCallTimeMs);

                DateTime reqTime;
                bool valid = DateTime.TryParse(values[(UInt32)CSVValueIndex.RequestTime], null, System.Globalization.DateTimeStyles.RoundtripKind, out reqTime);
                if (valid)
                {
                    item.m_reqTimeUTC = (UInt64)reqTime.ToFileTimeUtc();
                }

                bool.TryParse(values[(UInt32)CSVValueIndex.IsGet], out item.m_isGet);
                UInt32.TryParse(values[(UInt32)CSVValueIndex.Id], out item.m_id);
                bool.TryParse(values[(UInt32)CSVValueIndex.IsShoulderTap], out item.m_isShoulderTap);
                UInt64.TryParse(values[(UInt32)CSVValueIndex.ChangeNumber], out item.m_changeNumber);
                item.m_sessionReferenceUriPath = values[(UInt32)CSVValueIndex.SessionReferenceUriPath];
                bool.TryParse(values[(UInt32)CSVValueIndex.IsInGameEvent], out item.m_isInGameEvent);
                item.m_eventName = values[(UInt32)CSVValueIndex.EventName];
                item.m_playerSessionId = values[(UInt32)CSVValueIndex.PlayerSessionId];
                UInt16.TryParse(values[(UInt32)CSVValueIndex.EventVersion], out item.m_version);
                item.m_dimensions = values[(UInt32)CSVValueIndex.Dimensions];
                item.m_measurements = values[(UInt32)CSVValueIndex.Measurements];
                item.m_breadCrumb = values[(UInt32)CSVValueIndex.BreadCrumb];
            }
            catch (Exception)
            {
                Console.WriteLine("Error Parsing a CSV Item.");
                return null;
            }

            return item;
        }

        public static ServiceCallItem FromFiddlerFrame(UInt32 frameId, ZipArchiveEntry cFileStream, ZipArchiveEntry mFileStream, ZipArchiveEntry sFileStream, Func<WebHeaderCollection, bool> filterCallback)
        {

            ServiceCallItem frame = new ServiceCallItem();
            frame.m_id = frameId;

            // Read the client part of the frame (###_c.txt)
            using (var cFileMemory = Utils.DecompressToMemory(cFileStream))
            {
                using (var cFile = new BinaryReader(cFileMemory))
                {
                    var fileLine = cFile.ReadLine();

                    var firstLineSplit = fileLine.Split(' ');

                    // CONNECT Frames should not be in the analysis.
                    if (firstLineSplit[0] == "CONNECT")
                    {
                        System.Diagnostics.Debug.WriteLine("CONNECT Frames should not be in the analysis.");
                        return null;
                    }

                    // Fiddler Test Frames can cause LTA to break.  This filters out those fames.
                    if (firstLineSplit[1].StartsWith("http:///", true, null))
                    {
                        System.Diagnostics.Debug.WriteLine("Fiddler Test Frames should not be in the analysis.");
                        return null;
                    }

                    frame.m_isGet = firstLineSplit[0].Equals("GET");
                    frame.m_method = firstLineSplit[0];

                    // Extract the XUID (if any) from the first line of the client side of the frame
                    // POST https://userpresence.xboxlive.com/users/xuid(2669321029139235)/devices/current HTTP/1.1	
                    frame.m_xboxUserId = Utils.GetXboxUserID(firstLineSplit[1]);

                    // Grab just the url from the line
                    frame.m_uri = firstLineSplit[1];

                    // Read the Request Headers
                    fileLine = cFile.ReadLine();
                    var reqHeaders = new WebHeaderCollection();
                    while (String.IsNullOrWhiteSpace(fileLine) == false)
                    {
                        try
                        {
                            reqHeaders.Add(fileLine);
                        }
                        catch (Exception)
                        {
                            // This will throw if a header value contains invalid characters
                            // Ignore and continue
                        }

                        fileLine = cFile.ReadLine();
                    }

                    System.Diagnostics.Debug.WriteLine("Analyzing " + frame.m_uri);
                    // Filter calls with headers
                    if (filterCallback!= null && !filterCallback(reqHeaders))
                    {
                        return null;
                    }

                    frame.m_host = reqHeaders["Host"];

                    // Read the Request Body
                    string contentEncoding = reqHeaders["Content-Encoding"];
                    if (!string.IsNullOrWhiteSpace(contentEncoding) && contentEncoding.Equals("deflate", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var memory = Utils.InflateData(cFile.ReadToEnd()))
                        {
                            using (var data = new BinaryReader(memory))
                            {
                                fileLine = Encoding.ASCII.GetString(data.ReadToEnd());
                            }
                        }
                    }
                    else
                    {
                        fileLine = Encoding.ASCII.GetString(cFile.ReadToEnd());
                    }

                    frame.m_reqHeader = reqHeaders.ToString();
                    frame.m_reqBody = fileLine;
                    frame.m_reqBodyHash = (UInt64)frame.m_reqBody.GetHashCode();
                }
            }

            // Read the frame metadata (###_m.xml)
            using(var mFile = new StreamReader(mFileStream.Open()))
            {
                String rawData = mFile.ReadToEnd();
                var xmldata = System.Xml.Linq.XDocument.Parse(rawData);

                var sessionTimers = xmldata.Element("Session").Element("SessionTimers");
                var reqTime = DateTime.Parse((String)sessionTimers.Attribute("ClientBeginRequest")).ToUniversalTime();
                frame.m_reqTimeUTC = (UInt64)reqTime.ToFileTimeUtc();

                var endTime = DateTime.Parse((String)sessionTimers.Attribute("ClientDoneResponse")).ToUniversalTime();
                frame.m_elapsedCallTimeMs = (UInt64)(endTime - reqTime).TotalMilliseconds;

                var sessionFlags = xmldata.Element("Session").Element("SessionFlags");
                
                foreach(var flag in sessionFlags.Descendants())
                {
                    if((String)flag.Attribute("N") == "x-clientip")
                    {
                        frame.m_consoleIP = (String)flag.Attribute("V");
                        frame.m_consoleIP = frame.m_consoleIP.Substring(frame.m_consoleIP.LastIndexOf(':') + 1);
                        break;
                    }
                }
            }

            //Read the server part of the frame(###_s.txt)
            using (var sFileMemory = Utils.DecompressToMemory(sFileStream))
            {
                using (var sFile = new BinaryReader(sFileMemory))
                {
                    var fileLine = sFile.ReadLine();

                    if (String.IsNullOrEmpty(fileLine) == false)
                    {
                        var statusCodeLine = fileLine.Split(' ');

                        frame.m_httpStatusCode = UInt32.Parse(statusCodeLine[1]);
                    }

                    // Read the Response Headers
                    var headers = new WebHeaderCollection();
                    fileLine = sFile.ReadLine();
                    while (!String.IsNullOrWhiteSpace(fileLine))
                    {
                        headers.Add(fileLine);
                        fileLine = sFile.ReadLine();
                    }

                    // Read the Response Body
                    string contentEncoding = headers["Content-Encoding"];
                    if (!string.IsNullOrWhiteSpace(contentEncoding) && contentEncoding.Equals("deflate", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var memory = Utils.InflateData(sFile.ReadToEnd()))
                        {
                            using (var data = new BinaryReader(memory))
                            {
                                fileLine = Encoding.ASCII.GetString(data.ReadToEnd());
                            }
                        }
                    }
                    else
                    {
                        fileLine = Encoding.ASCII.GetString(sFile.ReadToEnd());
                    }

                    frame.m_rspHeader = headers.ToString();

                    // Read the Response Body
                    frame.m_rspBody = fileLine;
                }
            }

            return frame;
        }
        #endregion
    }
}
