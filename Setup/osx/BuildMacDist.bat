@echo off
setlocal enabledelayedexpansion
if "%apsimx%"=="" (
	pushd %~dp0..\..>nul
	set "apsimx=!cd!"
	popd>nul
)

rem Delete all files from Windows' DeploymentSupport directory from bin
for /r %apsimx%\DeploymentSupport\Windows %%D in (*.dll) do (
	if exist %apsimx%\bin\Release\net472\%%~nD%%~xD (
		echo Deleting %apsimx%\bin\Release\net472\%%~nD%%~xD...
		del %apsimx%\bin\Release\net472\%%~nD%%~xD
	)
)

pushd %~dp0
set "PATH=%PATH%;C:\tools\cygwin\bin;C:\Utilities"
if Exist ApsimSetup.dmg Del ApsimSetup.dmg
if Exist Version.tmp Del Version.tmp
if not exist %apsimx%\bin\Release\net472\Models.exe exit /B 1

sigcheck64 -n -nobanner %apsimx%\bin\Release\net472\Models.exe > Version.tmp
set /p APSIM_VERSION=<Version.tmp
set issuenumber=%APSIM_VERSION:~-4,4%
del Version.tmp

if Exist .\MacBundle rmdir /S /Q .\MacBundle
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app\Contents
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\MacOS
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources
mkdir .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\bin

dos2unix .\Template\Contents\MacOS\ApsimNG>nul 2>&1
copy .\Template\Contents\MacOS\ApsimNG .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\MacOS\ApsimNG>nul
copy .\Template\Contents\Resources\ApsimNG.icns .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\ApsimNG.icns>nul
xcopy /S /I /Y /Q %apsimx%\Examples .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\Examples>nul
xcopy /S /I /Y /Q %apsimx%\ApsimNG\Resources\world .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\ApsimNG\Resources\world>nul
xcopy /S /I /Y /Q %apsimx%\Tests\UnderReview .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\UnderReview>nul
xcopy /I /Y /Q %apsimx%\bin\Release\net472\*.dll .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\bin>nul
xcopy /I /Y /Q %apsimx%\bin\Release\net472\*.exe .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\bin>nul
xcopy /I /Y /Q %apsimx%\ApsimNG\Assemblies\Mono.TextEditor.dll.config .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\bin>nul
xcopy /I /Y /Q %apsimx%\ApsimNG\Assemblies\webkit-sharp.dll .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\bin>nul
xcopy /I /Y /Q %apsimx%\bin\Release\net472\Models.xml .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources\bin>nul
xcopy /I /Y /Q %apsimx%\APSIM.Documentation\Resources\APSIM.bib .\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Resources>nul

set PLIST_FILE=.\MacBundle\APSIM%APSIM_VERSION%.app\Contents\Info.plist
(
echo ^<?xml version="1.0" encoding="UTF-8"?^>
echo ^<^^!DOCTYPE plist PUBLIC "-//Apple Computer//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd"^>
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
genisoimage -quiet -V APSIM%APSIM_VERSION% -D -R -apple -no-pad -file-mode 755 -dir-mode 755 -o ApsimSetup%issuenumber%.dmg MacBundle
rmdir /S /Q .\MacBundle
popd
exit /B 0
