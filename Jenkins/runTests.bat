@echo off
SETLOCAL EnableDelayedExpansion
pushd %~dp0..>nul
set apsimx=%cd%
popd>nul
set "bin=%apsimx%\Bin"

rem Add bin to path.
set "PATH=%PATH%;%bin%;C:\ProgramData\chocolatey\bin"

rem Copy files from DeploymentSupport.
copy /y %apsimx%\DeploymentSupport\Windows\Bin64\* %bin% >nul

rem Next, check which tests we want to run.
set unitsyntax=Unit
set uisyntax=UI
set prototypesyntax=Prototypes
set examplessyntax=Examples
set validationsyntax=Validation
set performancesyntax=Performance

if "%1"=="%unitsyntax%" (
	echo Running Unit Tests...
	nunit3-console.exe %bin%\UnitTests.dll
	goto :end
)

if "%1"=="%performancesyntax%" (
	set COMMIT_AUTHOR=%ghprbActualCommitAuthor%
	set PULL_ID=%ghprbPullId%
	curl -k https://www.apsim.info/APSIM.Builds.Service/Builds.svc/GetPullRequestDetails?pullRequestID=%PULL_ID% > temp.txt
	for /F "tokens=1-6 delims==><" %%I IN (temp.txt) DO SET FULLRESPONSE=%%K
	del temp.txt
	for /F "tokens=1-6 delims=," %%I IN ("%FULLRESPONSE%") DO SET DATETIMESTAMP=%%I
	
	echo Pull request ID: 	"%PULL_ID%"
	echo DateTime stamp: 	"%DATETIMESTAMP%"
	echo Commit author:		"%COMMIT_AUTHOR%"
	echo Running performance tests collector...
	%apsimx%\Docker\runtests\APSIM.PerformanceTests.Collector\APSIM.PerformanceTests.Collector.exe AddToDatabase %PULL_ID% %DATETIMESTAMP% %COMMIT_AUTHOR%
	set err=%errorlevel%
	if err neq 0 (
		echo APSIM.PerformanceTests.Collector did not run succecssfully!
		echo Log file:
		type %apsimx%\Docker\runtests\APSIM.PerformanceTests.Collector\PerformanceCollector.txt
	)
	exit /b %err%
)

if "%1"=="%uisyntax%" (
	rem Run UI Tests
    set uitests=%apsimx%\Tests\UserInterfaceTests
	if not exist "!uitests!" (
		echo "!uitests!" does not exist. Aborting...
		exit 1
	)
	if not exist "%bin%\ApsimNG.exe" (
		echo "%bin%\ApsimNG.exe" does not exist. Aborting...
		exit 1
	)
	
	echo Running UI Tests...
	start /wait %bin%\ApsimNG.exe !uitests!\CheckStandardToolBox.cs
	goto :end
)

if "%1"=="%prototypesyntax%" (
	set testdir=%apsimx%\Prototypes
	goto :tests
)

if "%1"=="%examplessyntax%" (
	set testdir=%apsimx%\Examples
	goto :tests
)

if "%1"=="%validationsyntax%" (
	set testdir=%apsimx%\Tests
	goto :tests
)

echo Usage: %0 ^<testswitch^>
echo Where testswitch is one of the following:
echo     %unitsyntax%
echo     %uisyntax%
echo     %prototypesyntax%
echo     %examplessyntax%
echo     %validationsyntax%

:tests
if not exist "%testdir%" (
	echo %testdir% does not exist. Aborting...
	exit 1
)
rem Modify registry entry so that DateTime format is dd/MM/yyyy.
echo Modifying system DateTime format...
reg add "HKCU\Control Panel\International" /v sShortDate /d "dd/MM/yyyy" /f

echo Deleting temp directory...
del %TEMP%\ApsimX /S /Q 1>nul 2>nul

echo Commencing simulations...
models.exe %testdir%\*.apsimx /Recurse
echo errorlevel: "%errorlevel%"