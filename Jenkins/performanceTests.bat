@echo off
setlocal enabledelayedexpansion
setlocal
if "%apsimx%"=="" (
	pushd %~dp0..>nul
	set "apsimx=!cd!"
	popd>nul
)
set COMMIT_AUTHOR=%ghprbActualCommitAuthor%
if "%COMMIT_AUTHOR%"=="" (
	rem ----- Seems to be a bug in the Jenkins extension "GitHub Pull Request Builder".
	rem ----- Somtimes this environment variable is not set. In this scenario, we have
	rem ----- a look at the git logs and set it manually ourselves. 😠
	git log -n 1 --pretty=%%an>"%tmp%\ghprbActualCommitAuthor.txt"
	set /p COMMIT_AUTHOR=<"%tmp%\ghprbActualCommitAuthor.txt"
	echo WARNING: Using COMMIT_AUTHOR Fallback; COMMIT_AUTHOR='!COMMIT_AUTHOR!'
)
set PULL_ID=%ghprbPullId%

set "http_code_file=%tmp%\http_code.txt"
set "resp_file=%tmp%\resp.txt"

curl -ks https://apsimdev.apsim.info/APSIM.Builds.Service/Builds.svc/GetPullRequestDetails?pullRequestID=%PULL_ID% -o "%resp_file%" -w "%%{http_code}">"%http_code_file%"

rem Check http code from server
set /p resp=<"%http_code_file%"
if "%resp%" neq "200" (
	echo failure while fetching info about pull request #%PULL_ID%: http response code is "%resp%". Full response from server:
	type "%resp_file%"
	exit /b 1
)

for /F "tokens=1-6 delims==><" %%I IN (%resp_file%) DO SET FULLRESPONSE=%%K
for /F "tokens=1-6 delims=," %%I IN ("%FULLRESPONSE%") DO SET DATETIMESTAMP=%%I

pushd %apsimx%\..
if not exist APSIM.PerformanceTests (
	echo Cloning APSIM.PerformanceTests...
	git clone https://github.com/APSIMInitiative/APSIM.PerformanceTests
)

rem Cleanup any modified files.
cd APSIM.PerformanceTests\APSIM.PerformanceTests.Collector
git checkout .
git clean -fdxq
git checkout master
git pull

echo Restoring nuget packages for APSIM.PerformanceTests.Collector...
nuget restore -verbosity quiet

echo Compiling APSIM.PerformanceTests.Collector...
msbuild /v:m /p:Configuration=Release /m APSIM.PerformanceTests.Collector.sln
copy /y "%apsimx%\DeploymentSupport\Windows\Bin32\sqlite3.dll" bin\Release\
echo Running performance tests collector...
bin\Release\APSIM.PerformanceTests.Collector.exe AddToDatabase %PULL_ID% %DATETIMESTAMP% %COMMIT_AUTHOR%

set err=%errorlevel%
if errorlevel 1 (
	echo APSIM.PerformanceTests.Collector did not run succecssfully!
	echo Pull request ID: 	"%PULL_ID%"
	echo DateTime stamp: 	"%DATETIMESTAMP%"
	echo Commit author:		"%COMMIT_AUTHOR%"
	echo Log file:
	type bin\Release\PerformanceCollector.txt
	exit /b 1
) else (
	echo Done.
)
popd
endlocal
exit /b %err%