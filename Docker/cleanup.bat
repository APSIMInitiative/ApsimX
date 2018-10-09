pushd ApsimX >nul
git checkout master
rem Don't cleanup nuget packages for now....this will be a problem in the long run!!
git clean -fxdq -e packages
git pull origin master
if %errorlevel% neq 0 (
	exit 1
)
popd >nul