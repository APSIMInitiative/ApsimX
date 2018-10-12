@echo off
set "PATH=%PATH%;C:\Utilities"
if Exist Apsim.deb Del Apsim.deb
rem Get the current version number
if Exist Version.tmp Del Version.tmp
if not exist %apsimx%\Bin\Models.exe exit /B 1
cd %apsimx%\Setup\Linux
rem Microsoft, in their infinite wisdom, decided that it would be a good idea for
rem sysinternals such as sigcheck to spawn a popup window the first time you run them,
rem which asks you to agree to their eula. To get around this, we just need to set a few
rem registry entries...
reg.exe ADD HKCU\Software\Sysinternals /v EulaAccepted /t REG_DWORD /d 1 /f
reg.exe ADD HKU\.DEFAULT\Software\Sysinternals /v EulaAccepted /t REG_DWORD /d 1 /f

sigcheck64 -n -nobanner %apsimx%\Bin\Models.exe > Version.tmp
set /p APSIM_VERSION=<Version.tmp
for /F "tokens=1,2 delims=." %%a in ("%APSIM_VERSION%") do (set SHORT_VERSION=%%a.%%b)
del Version.tmp

rem Create a clean set of output folders
if Exist .\DebPackage rmdir /S /Q .\DebPackage
mkdir .\DebPackage\DEBIAN
mkdir .\DebPackage\data\usr\local\bin
mkdir .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Bin
mkdir .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Examples

rem Create the main execution script; must have Unix newline
rem For some reason, running dos2unix on a file inside a mounted volume in a docker container
rem doesn't seem to work - some sort of permissions error. Instead, we run the command on a 
rem file in %tmp%, and then manually move this file where we want it.
cd DebPackage
echo 2.0> %tmp%\debian-binary
dos2unix %tmp%\debian-binary
move %tmp%\debian-binary .\

(
echo #!/bin/sh
echo exec /usr/bin/mono /usr/local/lib/apsim/%APSIM_VERSION%/Bin/ApsimNG.exe "$@"
)> %tmp%\apsim
dos2unix %tmp%\apsim
move %tmp%\apsim .\data\usr\local\bin\

(
echo #!/bin/sh
echo exec /usr/bin/mono /usr/local/lib/apsim/%APSIM_VERSION%/Bin/Models.exe "$@"
)> %tmp%\Models
dos2unix %tmp%\Models
move %tmp%\Models .\data\usr\local\bin\

cd %apsimx%\Setup\Linux
rem Delete all files from Windows' DeploymentSupport directory from Bin
for /r %apsimx%\DeploymentSupport %%D in (*.dll) do (
	if exist %apsimx%\Bin\%%~nD%%~xD (
		echo Deleting %apsimx%\Bin\%%~nD%%~xD...
		del %apsimx%\Bin\%%~nD%%~xD
	)
)

rem Copy the binaries and examples to their destinations
xcopy /S /I /Y /Q %apsimx%\Examples .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Examples
xcopy /I /Y /Q %apsimx%\Bin\*.dll .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Bin
xcopy /I /Y /Q %apsimx%\Bin\*.exe .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Bin
xcopy /I /Y /Q %apsimx%\ApsimNG\Assemblies\Mono.TextEditor.dll.config .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Bin
xcopy /I /Y /Q %apsimx%\ApsimNG\Assemblies\webkit-sharp.dll .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Bin
xcopy /I /Y /Q %apsimx%\ApsimNG\Assemblies\webkit-sharp.dll.config .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Bin
xcopy /I /Y /Q %apsimx%\Bin\Models.xml .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Bin
xcopy /I /Y /Q %apsimx%\APSIM.bib .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%

rem Determine the approximate size of the package
du.exe -k -s DebPackage > size.temp
for /F "tokens=1" %%i in (size.temp) do set INSTALL_SIZE=%%i
del size.temp

rem Create the control file
(
echo Package: apsim
echo Version: %APSIM_VERSION%
echo Section: science
echo Architecture: all
echo Priority: optional
echo Maintainer: APSIM Initiative ^<apsim@csiro.au^>
echo Homepage: www.apsim.info
echo Installed-Size: %INSTALL_SIZE%
echo Depends: gtk-sharp2 ^(^>=2.12^), libwebkitgtk-1.0-0, mono-runtime ^(^>=4.0^), mono-devel ^(^>=4.0^), sqlite3, libcanberra-gtk-module
echo Description: The Agricultural Production Systems Simulator.
echo ^ The Agricultural Production Systems sIMulator ^(APSIM^) is internationally recognised 
echo ^ as a highly advanced simulator of agricultural systems. It contains a suite of modules 
echo ^ which enable the simulation of systems that cover a range of plant, animal, soil, 
echo ^ climate and management interactions. 
)> .\DebPackage\DEBIAN\control

rem Calculate the md5sums
setlocal enabledelayedexpansion
cd DebPackage/data
fciv usr/local/bin/apsim | findstr /r /v "^//" >..\DEBIAN\md5sums
for %%a in (usr/local/lib/apsim/%APSIM_VERSION%/Bin/*) do (
for /F "tokens=1" %%i in ('md5sum "usr/local/lib/apsim/%APSIM_VERSION%/Bin/%%a"') do echo %%i usr/local/lib/apsim/%APSIM_VERSION%/Bin/%%a >> ..\DEBIAN\md5sums
)

for %%a in (usr/local/lib/apsim/%APSIM_VERSION%/Examples/*) do (
for /F "tokens=1" %%i in ('md5sum "usr/local/lib/apsim/%APSIM_VERSION%/Examples/%%a"') do echo %%i usr/local/lib/apsim/%APSIM_VERSION%/Examples/%%a >> ..\DEBIAN\md5sums
)

for %%a in (usr/local/lib/apsim/%APSIM_VERSION%/Examples/WeatherFiles/*) do (
for /F "tokens=1" %%i in ('md5sum "usr/local/lib/apsim/%APSIM_VERSION%/Examples/WeatherFiles/%%a"') do echo %%i usr/local/lib/apsim/%APSIM_VERSION%/Examples/WeatherFiles/%%a >> ..\DEBIAN\md5sums
)

rem Create the tarballs and ar them together
tar --mode=755 -cf ..\data.tar .
gzip ..\data.tar
cd ..\DEBIAN
tar -cf ..\control.tar .
gzip ..\control.tar
cd ..
if exist %apsimx%\Setup\Output\APSIMSetup.deb (
	echo Deleting old installer...
	del %apsimx%\Setup\Output\APSIMSetup.deb
)

if not exist %apsimx%\Setup\Output (
	mkdir %apsimx%\Setup\Output
)

dir
ar vr C:\APSIMSetup.deb debian-binary control.tar.gz data.tar.gz
move C:\APSIMSetup.deb %apsimx%\Setup\Output\
echo Finished creating installer.
exit /b %errorlevel%
