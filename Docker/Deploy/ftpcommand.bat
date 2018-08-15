@echo off
for /f "tokens=* delims=" %%f in ('dir /b /s documentation\*.pdf') do curl -T "%%f" "ftp://www.apsim.info/APSIM/Test/%%~nf%%~xf" --user admin:CsiroDMZ!