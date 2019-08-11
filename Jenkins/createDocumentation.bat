@echo off
call %~dp0..\Documentation\GenerateDocumentation.bat
if errorlevel 1 exit /b 1
call %~dp0..\Documentation\DocumentExamples.bat