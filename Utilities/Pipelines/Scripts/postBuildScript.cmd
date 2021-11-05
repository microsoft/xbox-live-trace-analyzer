echo Running postBuildScript.cmd
echo on

set

set TOOLS_BINARIESDIRECTORY=%BUILD_BINARIESDIRECTORY%
set TOOLS_SOURCEDIRECTORY=%BUILD_SOURCESDIRECTORY%


set TOOLS_DROP_LOCATION=%TOOLS_BINARIESDIRECTORY%\TraceAnalyzer
set TOOLS_DROP_LOCATION_VPACK=%TOOLS_DROP_LOCATION%\Tools-VPack
rmdir /s /q %TOOLS_DROP_LOCATION%
mkdir %TOOLS_DROP_LOCATION%
mkdir %TOOLS_DROP_LOCATION_VPACK%
mkdir %TOOLS_DROP_LOCATION%\ToolZip

setlocal
call %TOOLS_SOURCEDIRECTORY%\Utilities\Pipelines\Scripts\setBuildVersion.cmd


REM ------------------- VERSION SETUP BEGIN -------------------
for /f "tokens=2 delims==" %%G in ('wmic os get localdatetime /value') do set datetime=%%G
set DATETIME_YEAR=%datetime:~0,4%
set DATETIME_MONTH=%datetime:~4,2%
set DATETIME_DAY=%datetime:~6,2%

set SDK_POINT_NAME_YEAR=%DATETIME_YEAR%
set SDK_POINT_NAME_MONTH=%DATETIME_MONTH%
set SDK_POINT_NAME_DAY=%DATETIME_DAY%
set SDK_RELEASE_NAME=%SDK_RELEASE_YEAR:~2,2%%SDK_RELEASE_MONTH%
set LONG_SDK_RELEASE_NAME=%SDK_RELEASE_NAME%-%SDK_POINT_NAME_YEAR%%SDK_POINT_NAME_MONTH%%SDK_POINT_NAME_DAY%


REM ------------------- TOOLS BEGIN -------------------
set TOOLS_RELEASEDIRECTORY=%TOOLS_BINARIESDIRECTORY%\Release\AnyCPU

copy %TOOLS_RELEASEDIRECTORY%\XboxLiveTraceAnalyzer.exe         %TOOLS_DROP_LOCATION%\ToolZip\XblTraceAnalyzer.exe
copy %TOOLS_RELEASEDIRECTORY%\XboxLiveTraceAnalyzer.exe.config  %TOOLS_DROP_LOCATION%\ToolZip\XblTraceAnalyzer.exe.config
copy %TOOLS_RELEASEDIRECTORY%\XboxLiveTraceAnalyzer-ReadMe.docx %TOOLS_DROP_LOCATION%\ToolZip\XblTraceAnalyzer-ReadMe.docx

REM ------------------- OS VPACK BEGIN -------------------
copy %TOOLS_RELEASEDIRECTORY%\XboxLiveTraceAnalyzer.exe %TOOLS_DROP_LOCATION_VPACK%\XblTraceAnalyzer.exe

echo.
echo Done postBuildScript.cmd
echo.
endlocal
