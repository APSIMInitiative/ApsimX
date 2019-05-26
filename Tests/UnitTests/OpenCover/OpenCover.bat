rem ----- Set the %APSIM% variable based on the directory where this batch file is located
pushd %~dp0..\..\..
set APSIM=%CD%
popd

rem ----- Run OpenCover
"%APSIM%\packages\OpenCover.4.7.922\tools\OpenCover.Console.exe" -target:"%APSIM%\packages\NUnit.ConsoleRunner.3.10.0\tools\nunit3-console.exe" -targetargs:"%APSIM%\Bin\UnitTests.dll" -filter:"+[Models]Models*" -excludebyattribute:"System.CodeDom.Compiler.GeneratedCodeAttribute" -register:user -output:"OpenCover.Results.xml"

rem ----- Run ReportGenerator
%APSIM%\packages\ReportGenerator.4.1.5\net47\ReportGenerator.exe "-reports:OpenCover.Results.xml" "-targetdir:CodeCoverage"

pause