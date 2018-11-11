@echo off
rem Documents all .apsimx files under ApsimX\Examples\ManagerExamples
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
rmdir /S /Q "%documentation%\PDF" >nul 2>nul
for /R "%examples%\ManagerExamples" %%D in (*.apsimx) do (
	set "FileToDocument=%%D"
    echo Generating documentation for %%~nxD
    "%bin%\ApsimNG.exe" %documentation%\DocumentFile.cs
	if ERRORLEVEL 1 goto error
)
exit /b 0

:error
type "%bin%\errors.txt"
exit /B 1
endlocal