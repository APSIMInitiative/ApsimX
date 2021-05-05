@echo off

set MAJORVERSION=%1
set MINORVERSION=%2

wget http://bob.apsim.info/files/Apsim%MAJORVERSION%-r%MINORVERSION%.binaries.WINDOWS.X86_64.exe -O %JOB_DIR%\Apsim\apsimsfx.exe >> %JOB_DIR%\Model\autoruninfo.stdout 2>&1
if errorlevel 1 goto ERROR
set __COMPAT_LAYER=RUNASINVOKER && %JOB_DIR%\Apsim\apsimsfx.exe >> %JOB_DIR%\Model\autoruninfo.stdout 2>&1

move Temp\Model %JOB_DIR%\Apsim
move Temp\UserInterface %JOB_DIR%\Apsim
move Temp\Apsim.xml %JOB_DIR%\Apsim
rmdir Temp

del /q %JOB_DIR%\Apsim\apsimsfx.exe

dir >> %JOB_DIR%\Model\autoruninfo.stdout 2>&1
dir %JOB_DIR%\Model >> %JOB_DIR%\Model\autoruninfo.stdout 2>&1
dir %JOB_DIR%\Apsim >> %JOB_DIR%\Model\autoruninfo.stdout 2>&1

goto END
:ERROR
echo unable to download >> %JOB_DIR%\Model\autoruninfo.stdout 2>&1

:END
