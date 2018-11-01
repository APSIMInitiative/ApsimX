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

@curl -f "https://www.apsim.info/APSIM.Builds.Service/Builds.svc/AddBuild?pullRequestNumber=%1&ChangeDBPassword=%2"