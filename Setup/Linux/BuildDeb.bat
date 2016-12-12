@echo off
set PATH=C:\Jenkins\Utilities;%PATH%
if Exist Apsim.deb Del Apsim.deb
rem Get the current version number
if Exist Version.tmp Del Version.tmp
set APSIMX_BUILD_DIR="..\.."
if not exist %APSIMX_BUILD_DIR%\Bin\Models.exe exit /B 1
sigcheck64 -n -nobanner %APSIMX_BUILD_DIR%\Bin\Models.exe > Version.tmp
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
echo 2.0>.\DebPackage\debian-binary
dos2unix -q .\DebPackage\debian-binary

(
echo #!/bin/sh
echo exec /usr/bin/mono /usr/local/lib/apsim/%APSIM_VERSION%/Bin/ApsimNG.exe "$@"
)> .\DebPackage\data\usr\local\bin\apsim
dos2unix -q .\DebPackage\data\usr\local\bin\apsim

rem Copy the binaries and examples to their destinations
xcopy /S /I /Y /Q %APSIMX_BUILD_DIR%\Examples .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Examples
xcopy /I /Y /Q %APSIMX_BUILD_DIR%\Bin\*.dll .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Bin
xcopy /I /Y /Q %APSIMX_BUILD_DIR%\Bin\*.exe .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Bin
xcopy /I /Y /Q %APSIMX_BUILD_DIR%\ApsimNG\Assemblies\Mono.TextEditor.dll.config .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Bin
xcopy /I /Y /Q %APSIMX_BUILD_DIR%\ApsimNG\Assemblies\webkit-sharp.dll.config .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Bin
xcopy /I /Y /Q %APSIMX_BUILD_DIR%\ApsimNG\Assemblies\MonoMac.dll .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Bin
xcopy /I /Y /Q %APSIMX_BUILD_DIR%\ApsimNG\Bin\Models.xml .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%\Bin
xcopy /I /Y /Q %APSIMX_BUILD_DIR%\ApsimNG\APSIM.bib .\DebPackage\data\usr\local\lib\apsim\%APSIM_VERSION%

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
echo Maintainer: APSIM Initiative ^<apsim@daf.qld.gov.au^>
echo Homepage: www.apsim.info
echo Installed-Size: %INSTALL_SIZE%
echo Depends: gtk-sharp2 ^(^>=2.12^), libwebkit1.1-cil, mono-runtime ^(^>=4.0^), mono-devel ^(^>=4.0^), sqlite3
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
fciv "usr/local/lib/apsim/%APSIM_VERSION%/Bin/%%a" | findstr /r /v "^//" > Temp.out
set /p CHECKSUM_TMP=<Temp.out
for /F "tokens=1" %%i in ("!CHECKSUM_TMP!") do echo %%i usr/local/lib/apsim/%APSIM_VERSION%/Bin/%%a >> ..\DEBIAN\md5sums
del Temp.out
)

for %%a in (usr/local/lib/apsim/%APSIM_VERSION%/Examples/*) do (
fciv "usr/local/lib/apsim/%APSIM_VERSION%/Examples/%%a" | findstr /r /v "^//" > Temp.out
set /p CHECKSUM_TMP=<Temp.out
for /F "tokens=1" %%i in ("!CHECKSUM_TMP!") do echo %%i usr/local/lib/apsim/%APSIM_VERSION%/Examples/%%a >> ..\DEBIAN\md5sums
del Temp.out
)

for %%a in (usr/local/lib/apsim/%APSIM_VERSION%/Examples/WeatherFiles/*) do (
fciv "usr/local/lib/apsim/%APSIM_VERSION%/Examples/WeatherFiles/%%a" | findstr /r /v "^//" > Temp.out
set /p CHECKSUM_TMP=<Temp.out
for /F "tokens=1" %%i in ("!CHECKSUM_TMP!") do echo %%i usr/local/lib/apsim/%APSIM_VERSION%/Examples/WeatherFiles/%%a >> ..\DEBIAN\md5sums
del Temp.out
)

rem Create the tarballs and ar them together
tar --mode=755 -z -c -f ..\data.tar.gz .
cd ..\DEBIAN
tar zcf ..\control.tar.gz .
cd ..
ar r ..\Apsim.deb debian-binary control.tar.gz data.tar.gz
cd ..
rmdir /S /Q .\DebPackage
exit /B 0
