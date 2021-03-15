// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
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
    }

    internal class CertWarningReport : IReport
    {
        private bool m_json;
        public CertWarningReport(bool json)
        {
            m_json = json;
        }

        public void RunReport(String outputDirectory, IEnumerable<RuleResult> result, Dictionary<string, Tuple<string, string, string>> endpoints)
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
