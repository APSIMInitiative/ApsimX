@echo off
set "PATH=%PATH%;C:\tools\cygwin\bin;C:\Utilities"
if Exist ApsimSetup.dmg Del ApsimSetup.dmg
if Exist Version.tmp Del Version.tmp
if not exist %apsimx%\Bin\Models.exe exit /B 1

rem Microsoft, in their infinite wisdom, decided that it would be a good idea for
rem sysinternals such as sigcheck to spawn a popup window the first time you run them,
rem which asks you to agree to their eula. To get around this, we just need to set a few
rem registry entries...
reg.exe ADD HKCU\Software\Sysinternals /v EulaAccepted /t REG_DWORD /d 1 /f
reg.exe ADD HKU\.DEFAULT\Software\Sysinternals /v EulaAccepted /t REG_DWORD /d 1 /f

sigcheck64 -n -nobanner %apsimx%\Bin\Models.exe > Version.tmp
set /p APSIM_VERSION=<Version.tmp
set issuenumber=%APSIM_VERSION:~-4,4%
del Version.tmp

if Exist .\MacBundle rmdir /S /Q .\MacBundle
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app\Contents
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\MacOS
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Bin

dos2unix .\Template\Contents\MacOS\ApsimNG
copy .\Template\Contents\MacOS\ApsimNG .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\MacOS\ApsimNG
copy .\Template\Contents\Resources\ApsimNG.icns .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\ApsimNG.icns
xcopy /S /I /Y /Q %apsimx%\Examples .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Examples
xcopy /I /Y /Q %apsimx%\Bin\*.dll .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Bin
xcopy /I /Y /Q %apsimx%\Bin\*.exe .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Bin
xcopy /I /Y /Q %apsimx%\ApsimNG\Assemblies\Mono.TextEditor.dll.config .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Bin
xcopy /I /Y /Q %apsimx%\ApsimNG\Assemblies\webkit-sharp.dll .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Bin
xcopy /I /Y /Q %apsimx%\Bin\Models.xml .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Bin
xcopy /I /Y /Q %apsimx%\APSIM.bib .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources

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
@echo on
genisoimage -V APSIM%APSIM_VERSION% -D -R -apple -no-pad -file-mode 755 -dir-mode 755 -o ApsimSetup%issuenumber%.dmg MacBundle
rmdir /S /Q .\MacBundle
exit /B 0
