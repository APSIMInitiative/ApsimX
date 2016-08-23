@echo off

:: Generate model validation and configuration documentation
del errors.txt >nul 2>nul
rmdir /S /Q PDF >nul 2>nul
for /D %%D in (..\Tests\Validation\*) do (
    echo Generating PDF for model %%~nD
    set ModelName=%%~nD%%~xD
    ..\Bin\UserInterface.exe CreateModelDocumentation.cs
	if ERRORLEVEL 1 goto error
)
exit /B 0

:error
type errors.txt
exit /B 1
