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

rem Upload under review models' documentation
call :upload "%apsimx%\Documentation\PDF\UnderReview" ftp://apsimdev.apsim.info/APSIM/ApsimXFiles/UnderReview/
if errorlevel 1 exit /b 1
call :upload "%apsimx%\Documentation\PDF" ftp://apsimdev.apsim.info/APSIM/ApsimXFiles/
endlocal
exit /b

rem -------------------------------------------------------------------
rem ---------------------- Upload subroutine --------------------------
rem -------------------------------------------------------------------
rem
rem This subroutine will upload all PDF files in a directory (not
rem recursively) to a given URI.
rem
rem 2 arguments:
rem
rem 1. Directory to search under (required)
rem 2. Upload URI (required)
rem
rem -------------------------------------------------------------------
:upload
pushd "%1"
for %%D in (*.pdf) do (
	set "NEW_NAME=%%~nD%ISSUE_NUMBER%%%~xD"
	rename "%%D" "!NEW_NAME!"
	set "FILE=%%~dpD!NEW_NAME!"
	echo  Uploading "!FILE!"
	@curl -s -u !APSIM_SITE_CREDS! -T "!FILE!" %2
	if errorlevel 1 (
		echo error
		exit /b 1
	)
)
popd
exit /b
