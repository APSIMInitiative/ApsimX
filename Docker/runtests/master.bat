@echo off
SETLOCAL EnableDelayedExpansion
set apsimx=C:\ApsimX
rem First make sure ApsimX exists.
if not exist %apsimx% (
    echo %apsimx% does not exist. Aborting...
	exit 1
)

if exist %apsimx%\bin.zip (
	echo Unzipping %apsimx%\bin.zip...
	powershell -Command Expand-Archive -Path %apsimx%\bin.zip -DestinationPath %apsimx%\Bin -Force
	if %errorlevel% neq 0 (
		echo Error unzipping %apsimx%\bin.zip
		exit %errorlevel%
	)
)

set bin=%apsimx%\Bin
if not exist %bin% (
	echo %bin% does not exist. Aborting...
	exit 1
)

rem Copy files from DeploymentSupport.
copy /y %apsimx%\DeploymentSupport\Windows\Bin64\* %bin% >nul

rem Add bin to path.
set "PATH=%PATH%;%bin%;C:\ProgramData\chocolatey\bin"

rem Next, check which tests we want to run.
set unitsyntax=Unit
set uisyntax=UI
set prototypesyntax=Prototypes
set examplessyntax=Examples
set validationsyntax=Validation

if "%1"=="%unitsyntax%" (
	echo Running Unit Tests...
	nunit3-console.exe %bin%\UnitTests.dll
	goto :end
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

echo Usage: docker run -v Path/To/ApsimX:C:\ApsimX ^<imagename^> ^<testswitch^>
echo Where imagename is the name of the image ^(should be runapsimx^), and testswitch is one of the following:
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
del %TEMP%\ApsimX /S /Q 1>nul 2>nul
echo Commencing simulations...
models.exe %testdir%\*.apsimx /Recurse
echo errorlevel: "%errorlevel%"
set err=0
if errorlevel 1 (
	echo Errors found!
	exit %errorlevel%
)
if "%1"=="%validationsyntax%" (
	if "%RUN_PERFORMANCE_TESTS%"=="TRUE" (
		echo Pull request ID: 	"%PULL_ID%"
		echo DateTime stamp: 	"%DATETIMESTAMP%"
		echo Commit author:		"%COMMIT_AUTHOR%"
		echo Running performance tests collector...
		C:\ApsimX\Docker\runtests\APSIM.PerformanceTests.Collector\APSIM.PerformanceTests.Collector.exe AddToDatabase %PULL_ID% %DATETIMESTAMP% %COMMIT_AUTHOR%
		set err=%errorlevel%
		if errorlevel 1 (
			echo APSIM.PerformanceTests.Collector did not run succecssfully!
		) else (
			echo APSIM.PerformanceTests.Collector ran successfully!
		)
		echo Log file:
		type C:\ApsimX\Docker\runtests\APSIM.PerformanceTests.Collector\PerformanceCollector.txt
		exit %err%
	)
	if "%ARCHIVE_RESULTS%"=="TRUE" (
		cd %apsimx%
		7z a results.7z -r Tests\Validation\*.db
	)
)
:end
exit %errorlevel%