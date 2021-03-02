@echo off
pushd %~dp0..>nul
git reset .
git clean -xfdq
git checkout .
git fetch origin +refs/pull/*:refs/remotes/origin/pr/*
git checkout %sha1%

rem Check for merge conflicts with master branch
git fetch origin master
git merge --no-commit origin/master
if errorlevel 1 (
	git merge --abort
	echo Merge conflicts with master branch detected. Aborting build...
	exit /b 1
)
git merge --abort

popd>nul
exit /b 0