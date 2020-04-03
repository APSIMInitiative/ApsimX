@echo off
set "usage=Usage: %0 ^<pull request ID^> ^<Password^>"
if "%1"=="" (
	echo %usage%
	exit /b 1
)
if "%2"=="" (
	echo %usage%
	exit /b 1
)

rem Add build to builds database
@curl -f "https://apsimdev.apsim.info/APSIM.Builds.Service/Builds.svc/AddBuild?pullRequestNumber=%1&ChangeDBPassword=%2"
if errorlevel 1 exit /b 1

rem Trigger a netlify build
if "%NETLIFY_BUILD_HOOK%"=="" (
	echo Error - netlify build hook not supplied
	exit /b 1
)

@curl -X POST -d {} https://api.netlify.com/build_hooks/%NETLIFY_BUILD_HOOK%
if errorlevel 1 (
	echo Error triggering netlify build
	exit /b 1
)
