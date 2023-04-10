echo Running postBuildScriptVPack.cmd
echo on

rem Print all environment variables for debugging purposes
set

rem Set variables for tools directories
set TOOLS_BINARIESDIRECTORY=%BUILD_BINARIESDIRECTORY%
set TOOLS_DROP_LOCATION=%TOOLS_BINARIESDIRECTORY%\TraceAnalyzer
set TOOLS_DROP_LOCATION_VPACK=%TOOLS_DROP_LOCATION%\Tools-VPack

rem Copy the VPack manifest file to the tools drop location
copy %XES_VPACKMANIFESTDIRECTORY%\%XES_VPACKMANIFESTNAME% %TOOLS_DROP_LOCATION_VPACK%

echo.
echo Done postBuildScriptVPack.cmd
echo.

rem Clean up environment variables
endlocal

rem Exit the script with success code
exit /b
