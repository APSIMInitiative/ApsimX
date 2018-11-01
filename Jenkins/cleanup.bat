@echo off
pushd %~dp0..>nul
git reset .
git clean -xfdq
git checkout .
git fetch origin +refs/pull/*:refs/remotes/origin/pr/*
echo %sha1%
git checkout %sha1%
echo test
popd>nul