@echo off

set SDK_RELEASE_YEAR=2020
set SDK_RELEASE_MONTH=09

set /p buildVersionTxt= </BuildVersion.txt
if "%buildVersionTxt%"=="" set /p buildVersionTxt= <../BuildVersion.txt
if "%buildVersionTxt%"=="" set /p buildVersionTxt= <../../BuildVersion.txt
if "%buildVersionTxt%"=="" goto err

echo %buildVersionTxt%

set SDK_RELEASE_YEAR=%buildVersionTxt:~0,4%
set SDK_RELEASE_MONTH=%buildVersionTxt:~5,7%

set VCVARS_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\VC\Auxiliary\Build\vcvars64.bat"

if not exist %VCVARS_PATH% goto err

call %VCVARS_PATH%

goto done

:err
echo Couldn't find BuildVersion.txt or vcvars64.bat for Visual Studio 2019

:done
echo %SDK_RELEASE_YEAR% %SDK_RELEASE_MONTH%
