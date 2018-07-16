@echo off
SETLOCAL EnableDelayedExpansion
rem First make sure ApsimX exists.
set /a validcmd=0
set apsimx=C:\ApsimX
if not exist %apsimx% (
    echo %apsimx% does not exist. Aborting...
	exit 1
)

set bin=%apsimx%\Bin
if not exist %bin% (
	echo %bin% does not exist. Aborting...
	exit 1
)
cd %bin%

rem Next, check which tests we want to run.
set unitsyntax=Unit
set uisyntax=UI
set prototypesyntax=Prototypes
set examplessyntax=Examples
set validationsyntax=Validation
set simulationsyntax=Simulation

if "%1"=="%unitsyntax%" (
	set validcmd=1
	cd %apsimx%\Tests\UnitTests\bin\Debug
	%apsimx%\packages\NUnit.Runners.2.6.3\tools\nunit-console.exe UnitTests.dll /noshadow
	goto :end
)

if "%1"=="%uisyntax%" (
	rem Run UI Tests
    set uitests=%apsimx%\Tests\UserInterfaceTests
	if not exist "!uitests!" (
		echo "!uitests!" does not exist. Aborting...
		exit 1
	)
	set validcmd=1
	start /wait ApsimNG !uitests!\CheckStandardToolBox.cs
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
	set testdir=%apsimx%\Tests\Validation
	goto :tests
)

if "%1"=="%simulationsyntax%" (
	set testdir=%apsimx%\Tests\Simulation
	goto :tests
)


goto :end


:tests
if not exist "%testdir%" (
	echo %testdir% does not exist. Aborting...
	exit 1
)
set validcmd=1
del %TEMP%\ApsimX /S /Q 1>nul 2>nul
cd %bin%
models.exe %testdir%\*.apsimx /Recurse


:end
set err=%errorlevel%
if %validcmd% equ 0 (
	echo Usage: docker run -v Path/To/ApsimX:C:\ApsimX ^<imagename^> ^<testswitch^>
	echo Where imagename is the name of the image ^(should be runapsimx^), and testswitch is one of the following:
	echo     %unitsyntax%
	echo     %uisyntax%
	echo     %prototypesyntax%
	echo     %examplessyntax%
	echo     %validationsyntax%
	echo     %simulationsyntax%
)
exit %err%