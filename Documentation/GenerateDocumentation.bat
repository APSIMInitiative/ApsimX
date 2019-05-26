@echo off
setlocal enableDelayedExpansion
:: Generate model validation and configuration documentation
set apsimx=%~dp0..
if "%documentation%"=="" (
	set "documentation=%~dp0"
)

:: remove trailing backslash
if %documentation:~-1%==\ set documentation=%documentation:~0,-1%

del %apsimx%\Bin\errors.txt >nul 2>nul
for /D %%D in (%apsimx%\Tests\Validation\*) do (
    echo Generating PDF for model %%~nD
    set ModelName=%%~nD%%~xD
    %apsimx%\Bin\ApsimNG.exe %~dp0\CreateModelDocumentation.cs
	if ERRORLEVEL 1 goto error
)

:: Generate documentation for under review models and output
:: it at %documentation%\PDF\UnderReview
set "out_dir=%documentation%\PDF\UnderReview"
if not exist "%out_dir%" mkdir "%out_dir%"
call :document "%apsimx%\Tests\UnderReview" "%out_dir%"

endlocal
exit /B

:: --------------------------------------------------------------------
:: ------------------ Documentation subroutine ------------------------
:: --------------------------------------------------------------------
::
:: This subroutine will search for and document all .apsimx files under
:: a directory recursively.
::
:: 2 arguments:
::
:: 1. Directory to search under (required)
:: 2. Output directory (optional)
::
:: --------------------------------------------------------------------
:document
for /r %1 %%f in (*.apsimx) do (
	set "FileToDocument=%%f"
	echo Generating documentation for %%~nxf
	echo %~dp0
	"%bin%\ApsimNG.exe" "%documentation%\DocumentFile.cs"
	if errorlevel 1 goto error

	:: The script will always output .pdfs into %documentation%\PDF.
	:: If we were passed a second argument, use it as output directory.
	if "%2" neq "" (
		move "%documentation%\PDF\%%~nf.pdf" "%2\%%~nf.pdf"
	)
)

exit /b

:error
echo 1 or more errors encountered:
type %apsimx%\Bin\errors.txt
exit /B 1