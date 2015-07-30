@echo off

if EXIST errors.txt del errors.txt

rem Generate model validation and configuration documentation
for %%M in (Wheat OilPalm) do (
    echo Generating PDF for model %%M
    set ModelName=%%M
    ..\Bin\UserInterface.exe CreateModelDocumentation.cs
)

rem echo Generating DOxygen documentation for all of APSIM
rem "c:\Program Files\doxygen\bin\doxygen.exe" Doxyfile 1>Nul 2>Nul
