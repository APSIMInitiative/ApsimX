@echo off
setlocal
SETLOCAL EnableDelayedExpansion
setlocal enableExtensions

if "%apsimx%"=="" (
	pushd %~dp0..>nul
	set apsimx=!cd!
	popd>nul
)
set "bin=%apsimx%\Bin"

rem Next, check which tests we want to run.
set unitsyntax=Unit
set uisyntax=UI
set prototypesyntax=Prototypes
set examplessyntax=Examples
set validationsyntax=Validation

if "%1"=="%unitsyntax%" (
	echo Running Unit Tests...
	call :numTempFiles
	set count=!result!

	nunit3-console "%apsimx%\Bin\UnitTests.dll"
	if errorlevel 1 exit /b 1

	call :numTempFiles
	set count_after=!result!

	if not !count!==!count_after! (
		set /a n=!count_after!-!count!
		echo !n! .apsimx files were created by not deleted by unit tests
		exit /b 1
	)

	endlocal
	endlocal

	exit /b
)

if "%1"=="%uisyntax%" (
	goto :uitests
)

if "%1"=="%prototypesyntax%" (
	set testdir=%apsimx%\Prototypes\*.apsimx
	
	rem Extract restricted grapevine dataset
	set grapevine=%apsimx%\Prototypes\Grapevine
	echo %GRAPEVINE_PASSWORD%| 7z x !grapevine!\Observations.zip -o!grapevine!
	
	goto :tests
)

if "%1"=="%examplessyntax%" (
	set testdir=%apsimx%\Examples\*.apsimx
	goto :tests
)

if "%1"=="%validationsyntax%" (
	set "testdir=%apsimx%\Tests\Simulation\*.apsimx %apsimx%\Tests\UnderReview\*.apsimx %apsimx%\Tests\Validation\*.apsimx"
	rem Extract restricted soybean dataset
	set soybean=%apsimx%\Tests\UnderReview\Soybean
	echo %SOYBEAN_PASSWORD%| 7z x !soybean!\ObservedFACTS.7z -o!soybean!
	rem Extract restricted NPI wheat dataset
	set wheat=%apsimx%\Tests\Validation\Wheat
	echo %NPI_PASSWORD%| 7z x !wheat!\NPIValidation.7z -o!wheat!
	goto :tests
)

echo Usage: %0 ^<testswitch^>
echo Where testswitch is one of the following:
echo     %unitsyntax%
echo     %uisyntax%
echo     %prototypesyntax%
echo     %examplessyntax%
echo     %validationsyntax%

exit /b 1

:numTempFiles
setlocal
set count=0
for %%x in (%temp%\*.apsimx) do set /a count+=1
endlocal & set result=%count%
exit /b

:uitests
rem Run UI Tests
set "uitests=%apsimx%\Tests\UserInterfaceTests"
if not exist "%uitests%" (
	echo "%uitests%" does not exist. Aborting...
	exit /b 1
)
if not exist "%bin%\ApsimNG.exe" (
	echo "%bin%\ApsimNG.exe" does not exist. Aborting...
	exit /b 1
)
for /r "%uitests%" %%f in (*.cs) do (
	echo Running %%~nxf...
	start /wait %bin%\ApsimNG.exe "%%f"
	if errorlevel 1 (
		type errors.txt
		exit /b 1
	)
)
echo Successfully finished UI Tests.
exit /b 0
	
:tests
echo Deleting temp directory...
del %TEMP%\ApsimX /S /Q 1>nul 2>nul

echo Commencing simulations...
"%bin%\Models.exe" %testdir% /Recurse /RunTests /Verbose
endlocal
