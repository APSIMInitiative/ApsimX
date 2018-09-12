@echo off
pushd %~dp0.. >nul
set apsimx=%cd%
popd >nul

if exist %apsimx%\bin.zip (
	echo Unzipping %apsimx%\bin.zip...
	powershell -Command Expand-Archive -Path %apsimx%\bin.zip -DestinationPath %apsimx%\Bin -Force
	if %errorlevel% neq 0 (
		echo Error unzipping %apsimx%\bin.zip
		exit /b %errorlevel%
	)
)

if exist %apsimx%\results.7z (
	echo Unzipping %apsimx%\results.7z...
	pushd %apsimx% > nul
	7z x -y %apsimx%\results.7z
	popd >nul
	if errorlevel 1 (
		echo Error unzipping %apsimx%\results.7z
		exit /b %errorlevel%
	)
)

robocopy /e /NJS /np %apsimx%\DeploymentSupport\Windows\Bin64\lib %apsimx%\lib

call %apsimx%\Documentation\GenerateDocumentation.bat

docker build -t documentation %apsimx%\Docker\Documentation
docker run -m 12g --cpu-count %NUMBER_OF_PROCESSORS% --cpu-percent 100 -e NUMBER_OF_PROCESSORS -e ISSUE_NUMBER -e APSIM_SITE_CREDS -v %apsimx%:C:\ApsimX documentation