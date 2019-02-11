@echo off
rem Documents all .apsimx files under folders underneath ApsimX\Examples\
rem e.g. ApsimX\Examples\Wheat.apsimx will not be documented.
rem e.g. ApsimX\Examples\Test\Test.apsimx would be documented.
setlocal enableDelayedExpansion
if "%apsimx%"=="" (
	pushd %~dp0..>nul
	set "apsimx=!cd!"
	popd>nul
)

set "bin=%apsimx%\Bin"
set "documentation=%apsimx%\Documentation"
set "examples=%apsimx%\Examples"

del "%bin%\errors.txt" >nul 2>nul
rem Iterate over each directory under ApsimX\Examples
for /f %%f in ('dir %apsimx%\Examples /ad /b /on /s') do (
	set "subdir=%%f"
	call :documentdir
)
exit /b 0

:error
type "%bin%\errors.txt"
exit /B 1

:documentdir
for /r %subdir% %%a in (*.apsimx) do (
	set "FileToDocument=%%a"
	echo Generating documentation for %%~nxa
	"%bin%\ApsimNG.exe" %documentation%\DocumentFile.cs
	if errorlevel 1 goto error
)
exit /b

endlocal