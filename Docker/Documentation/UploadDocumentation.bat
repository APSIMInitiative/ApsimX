@echo off
setlocal EnableDelayedExpansion
set apsimx=C:\apsimx
if not exist %apsimx% (
	echo %apsimx% does not exist inside docker container. Aborting...
	exit 1
)

if not exist %apsimx%\Documentation\PDF (
	echo %apsimx%\Documentation\PDF does not exist inside docker container. Aborting...
	exit 1
)

for /r %apsimx%\Documentation\PDF %%D in (*.pdf) do (
	set "NEW_NAME=%%~nD%ISSUE_NUMBER%%%~xD"
	rename "%%D" "!NEW_NAME!"
	set "FILE=%%~dpD!NEW_NAME!"
	echo  Uploading "!FILE!"
	@curl -u !APSIM_SITE_CREDS! -T !FILE! ftp://www.apsim.info/APSIM/ApsimXFiles/
)