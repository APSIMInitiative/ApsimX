@echo off
echo ########### Add a green build to the builds database...
for /f "tokens=1-4 delims=/ " %%a in ('date /t') do (set DATE_STAMP=20%%c.%%b.%%c)
for /f "tokens=1-2 delims=: " %%a in ("%TIME%") do (set TIME_STAMP=%%a:%%b)
set DATETIMESTAMP=%DATE_STAMP%-%TIME_STAMP%
curl https://www.apsim.info/APSIM.Builds.Service/Builds.svc/AddBuild?pullRequestNumber=%PULL_ID%^&issueID=%ISSUE_NUMBER%^&issueTitle=%ISSUE_TITLE%^&Released=%RELEASED%^&buildTimeStamp=%DATETIMESTAMP%^&ChangeDBPassword=%PASSWORD%
if errorlevel 1 (
	echo Errors encountered!
)
exit %errorlevel%