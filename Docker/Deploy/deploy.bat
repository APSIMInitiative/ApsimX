@echo off
echo ########### Adding a green build to the builds database...
echo %PULL_ID%
echo curl "https://www.apsim.info/APSIM.Builds.Service/Builds.svc/AddBuild?pullRequestNumber=%PULL_ID%&ChangeDBPassword="
@curl "https://www.apsim.info/APSIM.Builds.Service/Builds.svc/AddBuild?pullRequestNumber=%PULL_ID%&ChangeDBPassword=%PASSWORD%"
if errorlevel 1 (
	echo Errors encountered!
)
exit %errorlevel%