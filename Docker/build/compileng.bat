@echo off

if not exist C:\bin (
	echo C:\bin does not exist. This directory must be mounted via the docker run -v switch.
	exit 1
)

rem Check if first argument was provided.
if "%1"=="" (
	echo Error: No URL provided.
	exit 1
)

rem Check if URL points to a git repo.
git ls-remote %1 > nul
if %errorlevel% neq 0 (
	echo Error: %1 does not point to a git repo.
	exit 1
) else (
	echo %1 appears to be a valid git repo.
)

rem Clone APSIM/APSIM.Shared
rem APSIM.Shared URL is hard-coded for now.
set apsimx=C:\ApsimX
git clone %1 %apsimx%
git clone https://github.com/APSIMInitiative/APSIM.Shared C:\APSIM.Shared

rem Call VS developer command prompt to setup environment
call "C:\BuildTools\Common7\Tools\VsDevCmd.bat"

rem Download nuget packages
echo Downloading NuGet packages.
cd %apsimx%
nuget restore

rem Copy files from deployment support
echo Copying DeploymentSupport files.
copy /y %apsimx%\DeploymentSupport\Windows\Bin64\* %apsimx%\Bin\

echo Building ApsimX.
msbuild /m %apsimx%\ApsimX.sln

rem Copying the binaries will modify errorlevel, so we need to save its value first.
set level=%errorlevel%

if %errorlevel% neq 0 (
	echo Build failed.
	exit %errorlevel%
)

copy /y %apsimx%\Bin\* C:\bin\
echo Build succeeded.
exit %level%