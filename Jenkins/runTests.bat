@echo off
setlocal
SETLOCAL EnableDelayedExpansion
if "%apsimx%"=="" (
	pushd %~dp0..>nul
	set apsimx=!cd!
	popd>nul
)
set "bin=%apsimx%\Bin"

rem Add bin to path.
set "PATH=%PATH%;%bin%"

rem Copy files from DeploymentSupport.
copy /y %apsimx%\DeploymentSupport\Windows\Bin64\* %bin% >nul

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
	exit /b
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

echo Deleting temp directory...
del %TEMP%\ApsimX /S /Q 1>nul 2>nul

echo Commencing simulations...
models.exe %testdir%\*.apsimx /Recurse
echo Done. exit code=%errorlevel%
endlocal