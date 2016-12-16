using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XboxLiveTrace
{

    internal class CertWarning
    {
        public string XRName;
        public string Requirement;
        public string Remark;
        public string Intent;
        public List<Violation> Violations;
    }

    internal class CertWarningReport : IReport
    {
        private bool m_json;
        public CertWarningReport(bool json)
        {
            m_json = json;
        }

        public void RunReport(String outputDirectory, IEnumerable<RuleResult> result, Dictionary<string, Tuple<string, string>> endpoints, bool upToDate, string latestVersion)
        {
            List<CertWarning> warningResults = new List<CertWarning>();

            // XR-049: Rich Presence
            var x49results = result.Where(r => r.RuleName == "XR049Rule");
            bool x49violation = false;
            // If no results, treat as violation.
            if (x49results.Count() == 0)
            {
                x49violation = true;
            }
            else
            {
                var x49result = x49results.First();
                x49violation = x49result.ViolationCount > 0;
            }

            // Add XR 049 warning
            if (x49violation)
            {
                warningResults.Add(new CertWarning
                {
                    XRName = "XR-049: Rich Presence",
                    Requirement = "Games must update a user’s presence information to accurately reflect his or her state.",
                    Remark = "The rich presence strings (including localized versions) must be configured in the Xbox service. Then titles must set rich presence strings by calling the SetPresenceAsync or set_presence API. <br/><br/> For more information about rich presence strings, see “Rich Presence Strings Overview” in the Xbox One Development Kit or Xbox Application Development Kit documentation.",
                    Intent = "Provide up-to-date information regarding what users are doing in a title. It's a form of promotion for the title and promotes social interaction by giving other users context into what their friends are doing."
                });
            }

            // If we saw sessiondirectory traffic, then we assume you're  using multiplayer.
            bool multiplayerUsed = result.Any(r => r.Endpoint == "sessiondirectory.xboxlive.com" && r.RuleName == "CallRecorder");

            if (multiplayerUsed)
            {
                //"XRName":"XR-053: Instrumentation of Online Multiplayer Events"
                var x53results = result.Where(r => r.RuleName == "XR053Rule");
                bool x53violation = true;
                if (x53results.Count() > 0)
                {
                    var x53result = x53results.First();

                    if (x53result.ViolationCount > 0)
                    {
                        warningResults.Add(new CertWarning
                        {
                            XRName = "XR-053: Instrumentation of Online Multiplayer Events",
                            Requirement = "All games that support online multiplayer gameplay must properly instrument the events of the class IGMRS and IGMRE, commonly known as MultiplayerRoundStart and MultiplayerRoundEnd common event schemas.",
                            Remark = "Events that use the MultiplayerRoundStart and MultiplayerRoundEnd event schemas are used to compute user reputation and multiplayer usage reporting, including the number of multiplayer sessions played and the cumulative duration of sessions. <br/><br/> Titles must log the MultiplayerRoundStart event only when the user is engaged in an online multiplayer game session or round. Multiplayer Game Session is defined as an instance of synchronous gameplay in which two or more users participate across unique Xbox 360 or Xbox One consoles. Titles must not log multiplayer round events during asynchronous gameplay session. The MultiplayerRoundEnd event should be called when the user is no longer engaged. For example, MultiplayerRoundStart should be used when a gameplay round begins, and MultiplayerRoundEnd at the end of the gameplay round. Lobby wait time before or after rounds must not be included. \r\n Every pair of MultiplayerRoundStart and MultiplayerRoundEnd common events must include a valid XUID and MultiplayerCorrelationID. The MultiplayerCorrelationID field has to be retrieved from the user’s current session from the MPSD service. <br/><br/> For details about the proper instrumentation of these events, see “Xbox Live and Online Services (in “Xbox Data Platform” > “Events” > “Required Events and Stats”) in the Xbox One Development Kit or Xbox Application Development Kit documentation.Please notice that if you rename and/or create a custom event on the IGMRS or IGMRE class, those events are subject to the same requirements as stated above.",
                            Intent = "Properly instrumenting these events will ensure that users’ online reputations and multiplayer usage KPIs are accurately calculated for your title.",
                            Violations = x53result.Violations
                        });
                    }
                }
            }

            using (var output = new StreamWriter(Path.Combine(outputDirectory, m_json ? "certWarning.json" : "certWarning.js")))
            {
                if (!m_json)
                {
                    output.Write("warnings = ");
                }
                
                using (var jsonWriter = new JsonTextWriter(output))
                {
                    jsonWriter.Formatting = Formatting.Indented;
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(jsonWriter, warningResults);
                }
            }
        }

    }
}
