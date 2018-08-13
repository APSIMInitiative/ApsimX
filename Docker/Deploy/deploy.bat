@echo off
echo Uploading Installations...
call C:\Jenkins\ftpcommand.bat %Issue_Number%
echo.
echo ########### Add a green build to DB
set /p PASSWORD=<C:\Jenkins\ChangeDBPassword.txt
curl -k https://www.apsim.info/APSIM.Builds.Service/Builds.svc/AddGreenBuild?pullRequestNumber=%ghprbPullId%^&buildTimeStamp=%DATETIMESTAMP%^&changeDBPassword=%PASSWORD%
