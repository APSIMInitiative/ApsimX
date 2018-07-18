@echo off
SETLOCAL EnableDelayedExpansion

set apsimx=C:\ApsimX

rem First make sure ApsimX exists.
if not exist %apsimx% (
    echo %apsimx% does not exist. Aborting...
	exit 1
)
	dir C:\
	dir C:\ApsimX
	dir C:\ApsimX\Bin
	
if exist %apsimx%\bin.zip (
	echo Unzipping %apsimx%\bin.zip
	
	powershell -Command Expand-Archive -Path %apsimx%\bin.zip -DestinationPath %apsimx%\Bin -Force
	dir C:\
	dir C:\ApsimX
	dir C:\ApsimX\Bin
)

set bin=%apsimx%\Bin
if not exist %bin% (
	echo %bin% does not exist. Aborting...
	exit 1
)

rem Copy files from DeploymentSupport.
copy /y %apsimx%\DeploymentSupport\Windows\Bin64\* %bin% >nul

rem Add bin to path.
set "PATH=%PATH%;%bin%"

rem Next, check which tests we want to run.
set unitsyntax=Unit
set uisyntax=UI
set prototypesyntax=Prototypes
set examplessyntax=Examples
set validationsyntax=Validation
set simulationsyntax=Simulation

if "%1"=="%unitsyntax%" (
	%apsimx%\packages\NUnit.Runners.2.6.3\tools\nunit-console.exe %apsimx%\Tests\UnitTests\bin\Debug\UnitTests.dll /noshadow
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
	reg add "HKCU\Control Panel\International" /V sShortDate /T REG_SZ /D dd/MM/yy /F
	set testdir=%apsimx%\Prototypes
	goto :tests
)

if "%1"=="%examplessyntax%" (
	set testdir=%apsimx%\Examples
	goto :tests
)

if "%1"=="%validationsyntax%" (
	set testdir=%apsimx%\Tests\Validation
	goto :tests
)

if "%1"=="%simulationsyntax%" (
	set testdir=%apsimx%\Tests\Simulation
	goto :tests
)

echo Usage: docker run -v Path/To/ApsimX:C:\ApsimX ^<imagename^> ^<testswitch^>
echo Where imagename is the name of the image ^(should be runapsimx^), and testswitch is one of the following:
echo     %unitsyntax%
echo     %uisyntax%
echo     %prototypesyntax%
echo     %examplessyntax%
echo     %validationsyntax%
echo     %simulationsyntax%


:tests
if not exist "%testdir%" (
	echo %testdir% does not exist. Aborting...
	exit 1
)
rem Modify registry entry so that DateTime format is dd/MM/yyyy.
reg add "HKCU\Control Panel\International" /v sShortDate /d "dd/MM/yyyy" /f
del %TEMP%\ApsimX /S /Q 1>nul 2>nul
cd %bin%
models.exe %testdir%\*.apsimx /Recurse

:end