rem See https://microsoft.sharepoint.com/teams/osg_xboxtv/xengsrv/SitePages/Extensibility%20Hooks.aspx for details
rem if '%TFS_IsFirstBuild%' NEQ 'True' goto done
echo Running preCompileScript.cmd

call %TFS_SourcesDirectory%\setBuildVersion.cmd

rem format release numbers
FOR /F "TOKENS=1 eol=/ DELIMS=. " %%A IN ("%TFS_VersionNumber%") DO SET SDK_POINT_NAME_YEARMONTH=%%A
FOR /F "TOKENS=2 eol=/ DELIMS=. " %%A IN ("%TFS_VersionNumber%") DO SET SDK_POINT_NAME_DAYVER=%%A
set SDK_POINT_NAME_YEAR=%SDK_POINT_NAME_YEARMONTH:~0,2%
set SDK_POINT_NAME_MONTH=%SDK_POINT_NAME_YEARMONTH:~2,2%
set SDK_POINT_NAME_DAY=%SDK_POINT_NAME_DAYVER:~0,2%
set SDK_POINT_NAME_VER=%SDK_POINT_NAME_DAYVER:~2,9%

set SDK_RELEASE_NAME=%SDK_RELEASE_YEAR:~2,2%%SDK_RELEASE_MONTH%
set LONG_SDK_RELEASE_NAME=%SDK_RELEASE_NAME%-%SDK_POINT_NAME_YEAR%%SDK_POINT_NAME_MONTH%%SDK_POINT_NAME_DAY%-%SDK_POINT_NAME_VER%
set NUGET_VERSION_NUMBER=%SDK_RELEASE_YEAR%.%SDK_RELEASE_MONTH%.%SDK_POINT_NAME_YEAR%%SDK_POINT_NAME_MONTH%%SDK_POINT_NAME_DAY%.%SDK_POINT_NAME_VER%

set TOOL_VERSION_FILE=%TFS_SourcesDirectory%\Tools\XBLTraceAnalyzer\RulesEngine\VersionInfo.cs
set TOOL_RULES_FILE=%TFS_SourcesDirectory%\Tools\XBLTraceAnalyzer\rules.json

del %TOOL_VERSION_FILE%
echo namespace XboxLiveTrace> %TOOL_VERSION_FILE%
echo {>> %TOOL_VERSION_FILE%
echo     class VersionInfo>> %TOOL_VERSION_FILE%
echo     {>> %TOOL_VERSION_FILE%
echo         public const System.String Version = "%NUGET_VERSION_NUMBER%";>> %TOOL_VERSION_FILE%
echo     }>> %TOOL_VERSION_FILE%
echo }>> %TOOL_VERSION_FILE%
type %TOOL_VERSION_FILE%

%TFS_SourcesDirectory%\Utilities\FindAndReplace.exe %TOOL_RULES_FILE% 1.0.0.0 %NUGET_VERSION_NUMBER%
type %TOOL_RULES_FILE%

echo Done preCompileScript.cmd
:done