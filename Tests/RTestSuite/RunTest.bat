@echo off
exit 1
"C:\Program Files\R\R-3.0.1\bin\x64\RScript" Tests\RTestSuite\RunTest.R %OS% %BUILD_NUMBER%
