@echo off
setlocal
:: Generate model validation and configuration documentation
set apsimx=%~dp0..
del %apsimx%\Bin\errors.txt >nul 2>nul
rmdir /S /Q PDF >nul 2>nul
for /D %%D in (%apsimx%\Tests\Validation\*) do (
    echo Generating PDF for model %%~nD
    set ModelName=%%~nD%%~xD
    %apsimx%\Bin\ApsimNG.exe %~dp0\CreateModelDocumentation.cs
	if ERRORLEVEL 1 goto error
)
exit /B 0

:error
type %apsimx%\Bin\errors.txt
exit /B 1
endlocal