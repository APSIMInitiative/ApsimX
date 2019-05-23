@echo off
setlocal enableDelayedExpansion
:: Generate model validation and configuration documentation
set apsimx=%~dp0..
if "%documentation%"=="" (
	set "documentation=%~dp0"
)
del %apsimx%\Bin\errors.txt >nul 2>nul
for /D %%D in (%apsimx%\Tests\Validation\*) do (
    echo Generating PDF for model %%~nD
    set ModelName=%%~nD%%~xD
    %apsimx%\Bin\ApsimNG.exe %~dp0\CreateModelDocumentation.cs
	if ERRORLEVEL 1 goto error
)

call :document "%apsimx%\Tests\UnderReview"

exit /B 0

:document
for /r %1 %%f in (*.apsimx) do (
	set "FileToDocument=%%f"
	echo Generating documentation for %%~nxf
	"%bin%\ApsimNG.exe" "%documentation%\DocumentFile.cs"
	if errorlevel 1 goto error
)

exit /b

:error
echo 1 or more errors encountered:
type %apsimx%\Bin\errors.txt
exit /B 1
endlocal