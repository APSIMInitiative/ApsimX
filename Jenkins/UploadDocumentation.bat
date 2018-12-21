@echo off
setlocal
setlocal EnableDelayedExpansion
if "%apsimx%"=="" (
	pushd %~dp0..>nul
	set apsimx=!cd!
	popd>nul
)

if not exist %apsimx%\Documentation\PDF (
	echo Error: %apsimx%\Documentation\PDF does not exist. Have you run %apsimx%\Documentation\GenerateDocumentation.bat?
	exit 1
)

for /r %apsimx%\Documentation\PDF %%D in (*.pdf) do (
	set "NEW_NAME=%%~nD%ISSUE_NUMBER%%%~xD"
	rename "%%D" "!NEW_NAME!"
	set "FILE=%%~dpD!NEW_NAME!"
	echo  Uploading "!FILE!"
	@curl -s -u !APSIM_SITE_CREDS! -T !FILE! ftp://www.apsim.info/APSIM/ApsimXFiles/
)
endlocal