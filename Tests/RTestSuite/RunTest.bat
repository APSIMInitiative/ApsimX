@echo off

rem Go back to the ApsimX root directory.
cd ..\..

"C:\Program Files\R\R-3.0.1\bin\Rscript.exe" Tests\RTestSuite\RunTest.R %1 
pause