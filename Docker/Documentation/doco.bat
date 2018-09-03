@echo off
set apsimx=C:\ApsimX
if not exist %apsimx% (
	echo %apsimx% does not exist. Aborting...
)

if exist %apsimx%\bin.zip (
	echo Unzipping %apsimx%\bin.zip...
	powershell -Command Expand-Archive -Path %apsimx%\bin.zip -DestinationPath %apsimx%\Bin -Force
	if %errorlevel% neq 0 (
		echo Error unzipping %apsimx%\bin.zip
		exit %errorlevel%
	)
)
cd %apsimx%
if exist %apsimx%\results.7z (
	echo Unzipping %apsimx%\results.7z...
	7z x -y results.7z
	if errorlevel 1 (
		echo Error unzipping %apsimx%\results.7z
	)
)

if not exist %apsimx%\lib (
	robocopy /e /NJS /np %apsimx%\DeploymentSupport\Windows\lib %apsimx%\lib
)

dir %apsimx%\Bin
cd %apsimx%\Documentation
set FC_DEBUG=8191
call GenerateDocumentation.bat
cd %apsimx%
for /r %apsimx%\Documentation\PDF %%D in (*.pdf) do (
	set "NEW_NAME=%%~nD%ISSUE_NUMBER%%%~xD"
	rename "%%D" "%NEW_NAME%"
	echo Uploading %NEW_NAME%
	@curl -u %APSIM_SITE_CREDS% -T "%%~dpD%NEW_NAME%" ftp://www.apsim.info/APSIM/ApsimXFiles/
)