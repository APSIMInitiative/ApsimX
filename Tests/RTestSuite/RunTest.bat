rem @echo off

for /R Tests %%A in (*.apsimx) do (
  "C:\Program Files\R\R-3.0.1\bin\x64\RScript" Tests\RTestSuite\RunTest.R "%%~dpnA" %OS% %BUILD_NUMBER%
)

find "[1] FALSE" output.txt

IF ERRORLEVEL 1 (
  del output.txt
  exit /B 0
)

IF ERRORLEVEL 0 (
  del output.txt
  exit /B 1
)
