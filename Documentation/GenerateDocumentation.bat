                     @echo off

rem Generate model validation and configuration documentation
..\Bin\UserInterface.exe CreateModelDocumentation.cs

rem Generate DOxygen documentation for all of APSIM
"c:\Program Files\doxygen\bin\doxygen.exe" Doxyfile

echo Can now copy the html directory to www.apsim.info into 
echo d:\Websites\ApsimX\Help. Best to do this via remote desktop - quicker than FTP

pause