@echo off

:: Generate model validation and configuration documentation
for /D %%D in (D:\ApsimX\Tests\Validation\*) do (
    echo Generating PDF for model %%~nD
    set ModelName=%%~nD%%~xD
    ..\Bin\UserInterface.exe CreateModelDocumentation.cs
)
