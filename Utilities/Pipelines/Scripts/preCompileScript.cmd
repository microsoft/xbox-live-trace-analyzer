@echo off

setlocal enabledelayedexpansion

rem See https://microsoft.sharepoint.com/teams/osg_xboxtv/xengsrv/SitePages/Extensibility%20Hooks.aspx for details
rem if '%TFS_IsFirstBuild%' NEQ 'True' goto done

echo Running preCompileScript.cmd

call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\VC\Auxiliary\Build\vcvarsamd64_x86.bat"

set SDK_RELEASE_YEAR=2022
set SDK_RELEASE_MONTH=04

set "buildVersionTxt="
set /p buildVersionTxt=</BuildVersion.txt
if "%buildVersionTxt%"=="" set /p buildVersionTxt= <../BuildVersion.txt
if "%buildVersionTxt%"=="" set /p buildVersionTxt= <../../BuildVersion.txt
if "%buildVersionTxt%"=="" goto err

echo %buildVersionTxt%

set SDK_RELEASE_YEAR=!buildVersionTxt:~0,4!
set SDK_RELEASE_MONTH=!buildVersionTxt:~5,7!

rem format release numbers
set SDK_POINT_NAME_YEARMONTH=%TFS_VersionNumber:~0,4%
set SDK_POINT_NAME_DAYVER=%TFS_VersionNumber:~5,9%
set SDK_POINT_NAME_YEAR=%SDK_POINT_NAME_YEARMONTH:~2,2%
set SDK_POINT_NAME_MONTH=%SDK_POINT_NAME_YEARMONTH:~0,2%
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
goto :eof

:err
echo Couldn't find BuildVersion.txt
goto :eof
