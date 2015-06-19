@echo off

if EXIST errors.txt del errors.txt

rem Generate model validation and configuration documentation
for %%M in (Wheat OilPalm) do (
    echo Generating PDF for crop %%M
    set ModelName=%%M
    ..\Bin\UserInterface.exe CreateModelDocumentation.cs
    "C:\Program Files (x86)\wkhtmltopdf\wkhtmltopdf.exe" html\%%M\Index.html %%M.pdf
    rmdir /S /Q html\%%M
)

echo Generating DOxygen documentation for all of APSIM
"c:\Program Files\doxygen\bin\doxygen.exe" Doxyfile 1>Nul 2>Nul

echo Can now copy the html directory to \\IIS-EXT1\APSIM-Sites\APSIM\ApsimX

pause