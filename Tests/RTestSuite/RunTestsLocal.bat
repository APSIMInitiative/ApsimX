@echo off
cd ..\..\
IF NOT DEFINED BUILD_NUMBER (set BUILD_NUMBER=-1)
"C:\Program Files\R\R-3.0.1\bin\x64\RScript" Tests\RTestSuite\RunTest.R %OS% %BUILD_NUMBER% -l
set BUILD_NUMBER=
cd Tests\RTestSuite
pause