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
