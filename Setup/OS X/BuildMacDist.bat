@echo off
if Exist ApsimSetup.dmg Del ApsimSetup.dmg
if Exist Version.tmp Del Version.tmp
set APSIMX_BUILD_DIR="..\.."
if not exist %APSIMX_BUILD_DIR%\Bin\Models.exe exit /B 1
C:\Jenkins\Utilities\sigcheck64 -n -nobanner %APSIMX_BUILD_DIR%\Bin\Models.exe > Version.tmp
set /p APSIM_VERSION=<Version.tmp
for /F "tokens=1,2 delims=." %%a in ("%APSIM_VERSION%") do (set SHORT_VERSION=%%a.%%b)
del Version.tmp

if Exist .\MacBundle rmdir /S /Q .\MacBundle
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app\Contents
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\MacOS
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Bin

copy .\Template\Contents\MacOS\ApsimNG .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\MacOS\ApsimNG
copy .\Template\Contents\Resources\ApsimNG.icns .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\ApsimNG.icns
xcopy /S /I /Y /Q %APSIMX_BUILD_DIR%\Examples .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Examples
xcopy /I /Y /Q %APSIMX_BUILD_DIR%\Bin\*.dll .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Bin
xcopy /I /Y /Q %APSIMX_BUILD_DIR%\Bin\*.exe .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Bin
xcopy /I /Y /Q %APSIMX_BUILD_DIR%\ApsimNG\Assemblies\Mono.TextEditor.dll.config .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Bin
xcopy /I /Y /Q %APSIMX_BUILD_DIR%\ApsimNG\Assemblies\MonoMac.dll .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Bin
xcopy /I /Y /Q %APSIMX_BUILD_DIR%\ApsimNG\Assemblies\webkit-sharp.dll .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Bin

set PLIST_FILE=.\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Info.plist
(
echo ^<?xml version="1.0" encoding="UTF-8"?^>
echo ^<!DOCTYPE plist PUBLIC "-//Apple Computer//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd"^>
echo ^<plist version="1.0"^>
echo ^<dict^>
echo    ^<key^>CFBundleDevelopmentRegion^</key^>
echo    ^<string^>English^</string^>
echo    ^<key^>CFBundleExecutable^</key^>
echo    ^<string^>ApsimNG^</string^>
echo    ^<key^>CFBundleIconFile^</key^>
echo    ^<string^>ApsimNG^</string^>
echo    ^<key^>CFBundleIdentifier^</key^>
echo    ^<string^>au.csiro.apsim.apsimx^</string^>
echo    ^<key^>CFBundleInfoDictionaryVersion^</key^>
echo    ^<string^>6.0^</string^>
echo    ^<key^>CFBundlePackageType^</key^>
echo    ^<string^>APPL^</string^>
echo    ^<key^>CFBundleSignature^</key^>
echo    ^<string^>xmmd^</string^>
echo    ^<key^>NSAppleScriptEnabled^</key^>
echo    ^<string^>NO^</string^>
)>%PLIST_FILE%
echo    ^<key^>CFBundleName^</key^>>>%PLIST_FILE%
echo    ^<string^>APSIM%APSIM_VERSION%^</string^>>>%PLIST_FILE%
echo    ^<key^>CFBundleVersion^</key^>>>%PLIST_FILE%
echo    ^<string^>%APSIM_VERSION%^</string^>>>%PLIST_FILE%
echo    ^<key^>CFBundleShortVersionString^</key^>>>%PLIST_FILE%
echo    ^<string^>%SHORT_VERSION%^</string^>>>%PLIST_FILE%
echo ^</dict^>>>%PLIST_FILE%
echo ^</plist^>>>%PLIST_FILE%

C:\Jenkins\Utilities\genisoimage -V APSIM%APSIM_VERSION% -D -R -apple -no-pad -o ApsimSetup.dmg MacBundle
rmdir /S /Q .\MacBundle
exit /B 0
