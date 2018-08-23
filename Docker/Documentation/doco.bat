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

cd %apsimx%\Documentation
call GenerateDocumentation.bat
cd %apsimx%
for /r Tests\Validation %%D in (*.pdf) do ( 
	rename %%D %%~nD%ISSUE_NUMBER%%%~xD
	echo Uploading %%~nD%ISSUE_NUMBER%%%~xD
	@curl -u %APSIM_SITE_CREDS% -T "%%~dpnD%ISSUE_NUMBER%%%~xD" ftp://www.apsim.info/APSIM/ApsimXFiles/
)