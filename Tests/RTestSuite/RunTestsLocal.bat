@echo off
cd ..\..\
IF NOT DEFINED BUILD_NUMBER (set BUILD_NUMBER=-1)
"C:\Program Files\R\R-3.0.1\bin\x64\RScript" Tests\RTestSuite\RunTest.R %OS% %BUILD_NUMBER% -l > testresults.txt
if ERRORLEVEL 0 echo All tests passed 
if ERRORLEVEL 1 echo Some tests failed
set BUILD_NUMBER=
cd Tests\RTestSuite
pause