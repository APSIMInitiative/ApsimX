@echo off
setlocal
setlocal enabledelayedexpansion
rem Ensure we have an apsimx variable.
if "%apsimx%"=="" (
	pushd %~dp0..>nul
	set "apsimx=!cd!"
	popd>nul
)
set "setup=%apsimx%\Setup"
set "output=%setup%\Output"
set "art_file=%apsimx%\Jenkins\AsciiArt\%1.dat"

rem Display ascii art to user
if exist "%art_file%" (
	type "%art_file%"
)

rem Delete installer output directory and its contents if it exists.
if exist %setup%\Output (
	echo Removing old installers...
	rmdir /s /q %setup%\Output
)

rem Parse command line argument. 
rem It must be either windows, debian, or macos (case-sensitive).
set windowssyntax=windows
set debiansyntax=debian
set macossyntax=macos

rem Generate the installer if the user provided a valid 
if "%1"=="%windowssyntax%" goto :windows

if "%1"=="%debiansyntax%" goto :debian

if "%1"=="%macossyntax%" goto :macos

echo Usage: %0 ^(windows ^| debian ^| macos^)
goto :end

:getIssueNumber
rem Get the issue number for the pull request which triggered this build.
sigcheck64 -n -nobanner %apsimx%\bin\Release\net472\Models.exe > Version.tmp
set /p APSIM_VERSION=<Version.tmp
set ISSUE_NUMBER=%APSIM_VERSION:~-4,4%
set APSIM_VERSION=
del Version.tmp
exit /b

:windows
call :getIssueNumber
if exist C:\signapsimx call C:\signapsimx\sign.bat "%apsimx%\bin\Release\net472\Updater.exe"
echo Generating installer...
iscc /Q %setup%\apsimx.iss
set "file_name=ApsimSetup%ISSUE_NUMBER%.exe"
rename "%setup%\Output\ApsimSetup.exe" "!file_name!"
set "file_name=%output%\%file_name%"
if exist C:\signapsimx call C:\signapsimx\sign.bat "%file_name%"
goto :upload

:debian
call :getIssueNumber
echo Generating installer...
call %setup%\Linux\BuildDeb.bat
set "file_name=ApsimSetup%ISSUE_NUMBER%.deb"
rename "%setup%\Output\ApsimSetup.deb" "!file_name!"
set "file_name=%output%\%file_name%"
goto :upload

:macos
call :getIssueNumber
echo Generating installer...
call %setup%\osx\BuildMacDist.bat
set "file_name=%setup%\osx\ApsimSetup%ISSUE_NUMBER%.dmg"
goto :upload

:upload
if errorlevel 1 (
	echo Encountered an error while generating installer!
	exit /b 1
) else echo Done.
echo Uploading %file_name%...
@curl -s -u !APSIM_SITE_CREDS! -T %file_name% ftp://apsimdev.apsim.info/APSIM/ApsimXFiles/
if errorlevel 1 (
	echo Encountered an error while uploading %file_name%!
) else (
	echo Done.
)
goto :end

:end
endlocal