@echo off

for /R Tests %%A in (*.apsimx) do (
  "C:\Program Files\R\R-3.0.1\bin\x64\RScript" Tests\RTestSuite\RunTest.R "%%~dpnA" %OS% %BUILD_NUMBER%
  if ERRORLEVEL == 1 (echo exit 1)
)
