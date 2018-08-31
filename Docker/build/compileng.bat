@echo off
set apsimx=C:\ApsimX
if not exist %apsimx% (
	echo C:\ApsimX does not exist. This directory must be mounted via the docker run -v switch.
	exit 1
)

echo ########### Create an APSIM_VERSION (yyyy.mm.dd.###) environment variable.
curl -k https://www.apsim.info/APSIM.Builds.Service/Builds.svc/GetPullRequestDetails?pullRequestID=%PULL_ID% > temp.txt
FOR /F "tokens=1-6 delims==><" %%I IN (temp.txt) DO SET FULLRESPONSE=%%K
FOR /F "tokens=1-6 delims=-" %%I IN ("%FULLRESPONSE%") DO SET BUILD_TIMESTAMP=%%I
FOR /F "tokens=1-6 delims=," %%I IN ("%FULLRESPONSE%") DO SET DATETIMESTAMP=%%I
FOR /F "tokens=1-6 delims=," %%I IN ("%FULLRESPONSE%") DO SET ISSUE_NUMBER=%%J
set APSIM_VERSION=%BUILD_TIMESTAMP%.%ISSUE_NUMBER%

echo APSIM_VERSION=%APSIM_VERSION% > ApsimX\Bin\Build.properties
echo ISSUE_NUMBER=%ISSUE_NUMBER% >> ApsimX\Bin\Build.properties
echo DATETIMESTAMP=%DATETIMESTAMP% >> ApsimX\Bin\Build.properties
echo APSIM_VERSION=%APSIM_VERSION%
echo ISSUE_NUMBER=%ISSUE_NUMBER%
echo DATETIMESTAMP=%DATETIMESTAMP%
echo %DATETIMESTAMP% > ApsimX\datetimestamp.txt
echo ########### Insert the version number into AssemblyVersion.cs
echo using System.Reflection; > ApsimX\Models\Properties\AssemblyVersion.cs
echo [assembly: AssemblyTitle("APSIM %APSIM_VERSION%")] >> ApsimX\Models\Properties\AssemblyVersion.cs
echo [assembly: AssemblyVersion("%APSIM_VERSION%")] >> ApsimX\Models\Properties\AssemblyVersion.cs
echo [assembly: AssemblyFileVersion("%APSIM_VERSION%")] >> ApsimX\Models\Properties\AssemblyVersion.cs
echo Done. Version = %APSIM_VERSION%
echo.

rem Call VS developer command prompt to setup environment
call "C:\BuildTools\Common7\Tools\VsDevCmd.bat"

rem Download nuget packages
echo Downloading NuGet packages.
cd %apsimx%
nuget restore -verbosity quiet

rem Copy files from deployment support
echo Copying DeploymentSupport files.
copy /y %apsimx%\DeploymentSupport\Windows\Bin64\* %apsimx%\Bin\ >nul

echo Building ApsimX.
msbuild /m /v:m /p:Configuration=Release %apsimx%\ApsimX.sln

set error=%errorlevel%

if %error% equ 0 (
	rem We need to archive the binaries, but ApsimX\Bin is quite large. First we delete everything from DeploymentSupport,
	rem then we compress what's left.
	echo Compressing binaries.
	for /r %%i in (.\ApsimX\DeploymentSupport\Windows\Bin64\*) do del .\ApsimX\Bin\%%~nxi
	powershell -Command Compress-Archive %apsimx%\Bin\* -DestinationPath %apsimx%\bin.zip -CompressionLevel Optimal
)

exit %error%