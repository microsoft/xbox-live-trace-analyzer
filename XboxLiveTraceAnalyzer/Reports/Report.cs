using System;
using System.Collections.Generic;

namespace XboxLiveTrace
{
    // Interface for reporting the results of the analysis
    public interface IReport
    {
        // Parameters:
        //   - results:       IEnumerable of all of the results of from the Rules
        //   - endpointMap:   A mapping of all of the Xbox Live endpoint to the XSAPI method.
        //   - upToDate:      Identifies if this version of the Xbox Live Trace Analyzer is up to date
        //   - latestVersion: If upToDate is true, then this will be the current version.
        //                    If upToDate is true, then this will be the latest version that has been released.
        void RunReport(String outputDirectory, IEnumerable<RuleResult> results, Dictionary<String, Tuple<String, String>> endpointsMap, bool upToDate, String latestVersion);
    }
}
