@echo off
setlocal enabledelayedexpansion
set HOUR=%time:~0,2%
if "%HOUR:~0,1%" == " " set HOUR=0%HOUR:~1,1%
set MIN=%time:~3,2%
if "%MIN:~0,1%" == " " set MIN=0%MIN:~1,1%
for /f "tokens=1-4 delims=/ " %%a in ('date /t') do (set DATE_STAMP=20%%c.%%b.%%c)
set DATETIMESTAMP=%DATE_STAMP%-%HOUR%:%MIN%
echo DateTime=%DATETIMESTAMP%
echo ########### Adding a green build to the builds database...
@curl "https://www.apsim.info/APSIM.Builds.Service/Builds.svc/AddBuild?pullRequestNumber=%PULL_ID%^&ChangeDBPassword=%PASSWORD%"
if errorlevel 1 (
	echo Errors encountered!
)
exit %errorlevel%