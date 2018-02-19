set JOB_DIR=%AZ_BATCH_NODE_SHARED_DIR%\%AZ_BATCH_JOB_ID%
set MODEL_DIR=%JOB_DIR%\Model
set APSIM_DIR=%JOB_DIR%\Apsim

rem Delete existing folders
del /q /s %JOB_DIR%\Model
del /q /s %JOB_DIR%\Apsim

rem Create job specific folders
mkdir %JOB_DIR%
mkdir %JOB_DIR%\Model
mkdir %JOB_DIR%\Apsim

echo Beginning job prep > %MODEL_DIR%\jobprep.stdout
dir 1>> %MODEL_DIR%\jobprep.stdout 2>&1

rem Extract APSIM and the model files to the job specific folder
7za.exe x -y -o%JOB_DIR%\Model model.zip 1>> %MODEL_DIR%\jobprep.stdout 2>&1
7za.exe x -y -o%JOB_DIR%\Apsim apsim*.zip 1>> %MODEL_DIR%\jobprep.stdout 2>&1
del model.zip
del apsim*.zip

rem call a job-specific batch file if it exists
if exist %JOB_DIR%\Apsim\jobautorun.cmd call %JOB_DIR%\Apsim\jobautorun.cmd
if exist %JOB_DIR%\Model\jobautorun.cmd call %JOB_DIR%\Model\jobautorun.cmd

rem Remove all unused files for condor
del %JOB_DIR%\Model\*.bat
del %JOB_DIR%\Model\*.simulations
del %JOB_DIR%\Model\Apsim.pbs
del %JOB_DIR%\Model\Apsim.sub
del %JOB_DIR%\Model\CondorApsim.xml

dir %JOB_DIR% 1>> %MODEL_DIR%\jobprep.stdout 2>&1
dir %MODEL_DIR% 1>> %MODEL_DIR%\jobprep.stdout 2>&1
dir %APSIM_DIR% 1>> %MODEL_DIR%\jobprep.stdout 2>&1

rem Copy 7zip and AzCopy to shared location
copy /Y * %JOB_DIR% 1>> %MODEL_DIR%\jobprep.stdout 2>&1

