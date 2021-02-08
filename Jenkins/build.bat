@echo off
setlocal
setlocal enabledelayedexpansion

rem Display help syntax if necessary.
if "%1"=="/?" (
	echo Compiles Apsim
	echo Syntax: %0 [/r]
	echo     /r: build in release mode
	echo.    /?: show help information
	exit /b 0
)

rem Ensure apsimx environment variable is defined.
if "%apsimx%"=="" (
	pushd %~dp0..>nul
	set apsimx=!cd!
	popd>nul
)

rem Ensure solution file exists.
set "solution_file=%apsimx%\ApsimX.sln"
if not exist "%solution_file%" (
	echo Unable to find ApsimX.sln; attempted to locate it at %solution_file%
	exit /b 1
)

rem Set verbosity to minimal, don't display the logo, 
rem and use the multithreaded switch.
set "flags=-v m --nologo"

if "%1"=="/r" (
	rem We need to build in release mode.
	set "flags=%flags% -c Release"
)

rem Generate a version number.
call :getVersion

rem Build
dotnet build %flags% "%solution_file%"
endlocal
exit /b %errorlevel%

:getVersion
rem We generate a version number by calling a webservice.
if "%PULL_ID%"=="" (
   set PULL_ID=%ghprbPullId%
)
echo PULL_ID=%PULL_ID%
if "%PULL_ID%"=="" (
	echo Error: PULL_ID is not set.
	exit /b 1
)
echo Getting version number from web service...
curl -ks https://apsimdev.apsim.info/APSIM.Builds.Service/Builds.svc/GetPullRequestDetails?pullRequestID=%PULL_ID% > temp.txt
echo Done.
for /F "tokens=1-6 delims==><" %%I IN (temp.txt) DO SET FULLRESPONSE=%%K
del temp.txt
for /F "tokens=1-6 delims=-" %%I IN ("%FULLRESPONSE%") DO SET BUILD_TIMESTAMP=%%I
for /F "tokens=1-6 delims=," %%I IN ("%FULLRESPONSE%") DO SET DATETIMESTAMP=%%I
for /F "tokens=1-6 delims=," %%I IN ("%FULLRESPONSE%") DO SET ISSUE_NUMBER=%%J
set APSIM_VERSION=%BUILD_TIMESTAMP%.%ISSUE_NUMBER%
set YEAR=%date:~10,4%

echo APSIM_VERSION=%APSIM_VERSION% > %apsimx%\Bin\Build.properties
echo ISSUE_NUMBER=%ISSUE_NUMBER% >> %apsimx%\Bin\Build.properties
echo DATETIMESTAMP=%DATETIMESTAMP% >> %apsimx%\Bin\Build.properties

rem Display the version number.
echo.
echo APSIM_VERSION=%APSIM_VERSION%
echo ISSUE_NUMBER=%ISSUE_NUMBER%
echo DATETIMESTAMP=%DATETIMESTAMP%
echo.
rem Write this information to Models\Properties\AssemblyVersion.cs
echo using System.Reflection; > "%apsimx%\Models\Properties\AssemblyVersion.cs"
echo [assembly: AssemblyTitle("APSIM %APSIM_VERSION%")] >> "%apsimx%\Models\Properties\AssemblyVersion.cs"
echo [assembly: AssemblyVersion("%APSIM_VERSION%")] >> "%apsimx%\Models\Properties\AssemblyVersion.cs"
echo [assembly: AssemblyFileVersion("%APSIM_VERSION%")] >> "%apsimx%\Models\Properties\AssemblyVersion.cs"
echo [assembly: AssemblyCopyright("Copyright © APSIM Initiative %YEAR%")] >> "%apsimx%\Models\Properties\AssemblyVersion.cs"
