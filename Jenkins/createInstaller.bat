@echo off
setlocal
setlocal enabledelayedexpansion
rem Ensure we have an apsimx variable.
if "%apsimx%"=="" (
	pushd %~dp0..>nul
	set "apsimx=%cd%"
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

:windows
call :getIssueNumber
echo Generating installer...
iscc /Q %setup%\apsimx.iss
set "file_name=ApsimSetup%ISSUE_NUMBER%.exe"
rename "%setup%\Output\ApsimSetup.exe" "!file_name!"
set "file_name=%output%\%file_name%"
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
goto :upload
	
:upload
if errorlevel 1 (
	echo Error encountered while generating installer!
	exit /b 1
)
echo file_name=%file_name%
goto :end

:getIssueNumber

rem Get the issue number for the pull request which triggered this build.
set PULL_ID=%ghprbPullId%
echo Fetching issue number...
curl -ks https://www.apsim.info/APSIM.Builds.Service/Builds.svc/GetPullRequestDetails?pullRequestID=%PULL_ID% > temp.txt
for /F "tokens=1-6 delims==><" %%I IN (temp.txt) DO SET FULLRESPONSE=%%K
for /F "tokens=1-6 delims=," %%I IN ("%FULLRESPONSE%") DO SET ISSUE_NUMBER=%%J
echo ISSUE_NUMBER=%ISSUE_NUMBER%
exit /b

:end
endlocal