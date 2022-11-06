rem Args
set APSIM_FILE=%1
set SIMULATION=%2

rem These vars are set by the task provider
rem APSIM_STORAGE_CONTAINER_URL
rem APSIM_STORAGE_KEY

set JOB_DIR=%AZ_BATCH_NODE_SHARED_DIR%\%AZ_BATCH_JOB_ID%
set MODEL_DIR=%JOB_DIR%\Model
set APSIM_DIR=%JOB_DIR%\Apsim

rem Add apsim and tools to path
set PATH=%PATH%;%JOB_DIR%;%APSIM_DIR%

rem Copy the APSIM and model files to the working directory because APSIM tends
rem to lock the files preventing concurrent access
robocopy %MODEL_DIR% %AZ_BATCH_TASK_WORKING_DIR% /MIR /XF *.apsim
copy /Y %MODEL_DIR%\%APSIM_FILE% %AZ_BATCH_TASK_WORKING_DIR%

rem Execute APSIM

echo Running %APSIM_FILE% on %COMPUTERNAME% 1>> "%APSIM_FILE%".stdout 2>&1
echo Job Dir: %JOB_DIR% 1>> "%APSIM_FILE%".stdout 2>&1
echo Apsim Dir: %APSIM_DIR% 1>> "%APSIM_FILE%".stdout 2>&1
echo Task Work Dir: %AZ_BATCH_TASK_WORKING_DIR% 1>> "%APSIM_FILE%".stdout 2>&1
echo Batch Node Shared Dir: %AZ_BATCH_NODE_SHARED_DIR% 1>> "%APSIM_FILE%".stdout 2>&1
echo Batch Job ID: %AZ_BATCH_JOB_ID% 1>> "%APSIM_FILE%".stdout 2>&1

rem echo Run Info: 1>> "%APSIM_FILE%".stdout 2>&1
dir %JOB_DIR% 1>> "%APSIM_FILE%".stdout 2>&1
dir %APSIM_DIR% 1>> "%APSIM_FILE%".stdout 2>&1
dir %MODEL_DIR% 1>> "%APSIM_FILE%".stdout 2>&1

set APSIM_DIR_BQ=%APSIM_DIR:\=\\%
set APSIM=%APSIM_DIR%

set LC_ALL=en_GB.UTF-8

echo. 1>> "%APSIM_FILE%".stdout 2>&1
echo Environment: 1>> "%APSIM_FILE%".stdout 2>&1
set 1>> "%APSIM_FILE%".stdout 2>&1

for /f "delims=" %%a  in ("%APSIM_FILE%") do set "EXTN=%%~xa"
if /i "%EXTN%"==".apsimx" (
	rem new apsim
	echo Started at %date% %time% 1>> "%APSIM_FILE%".stdout 2>&1
	echo %APSIM_DIR%\Models.exe "%MODEL_DIR%\%APSIM_FILE%" 1>> "%APSIM_FILE%".stdout 2>&1
	
	%APSIM_DIR%\Models.exe "%MODEL_DIR%\%APSIM_FILE%" /Verbose 1>> "%APSIM_FILE%".stdout 2>&1
	
	dir "%MODEL_DIR%"
	echo Ended at %date% %time% 1>> "%APSIM_FILE%".stdout 2>&1
) else (
	rem this is for old apsim!
	if exist %APSIM_DIR%\Model\Apsim.exe (		
	  echo using %APSIM_DIR%\Model\Apsim.exe "%MODEL_DIR%\%APSIM_FILE%" "Simulation=/simulations/%SIMULATION%" 1>> %SIMULATION%_Apsim.stdout 2>&1
	  wmic datafile where name='%APSIM_DIR_BQ%\\Model\\Apsim.exe' get version /format:list | findstr "[0-9]" 1>> %SIMULATION%_Apsim.stdout 2>&1	  	  
    echo Started at %date% %time% 1>> %SIMULATION%_Apsim.stdout 2>&1	  
		%APSIM_DIR%\Model\Apsim.exe "%APSIM_FILE%" "/SimulationNameRegexPattern:%SIMULATION%" 1>> %SIMULATION%_Apsim.stdout 2>&1
    echo Ended at %date% %time% 1>> %SIMULATION%_Apsim.stdout 2>&1	  		
	)
	if exist %APSIM_DIR%\Apsim.exe ( 
	  echo using %APSIM_DIR%\Apsim.exe "%APSIM_FILE%" "Simulation=/simulations/%SIMULATION%" 1>> %SIMULATION%_Apsim.stdout 2>&1
	  wmic datafile where name='%APSIM_DIR_BQ%\\Apsim.exe' get version /format:list | findstr "[0-9]" 1>> %SIMULATION%_Apsim.stdout 2>&1	  	  
    echo Started at %date% %time% 1>> %SIMULATION%_Apsim.stdout 2>&1	  	  
		%APSIM_DIR%\Apsim.exe "%APSIM_FILE%" "Simulation=/simulations/%SIMULATION%" 1>> %SIMULATION%_Apsim.stdout 2>&1
    echo Ended at %date% %time% 1>> %SIMULATION%_Apsim.stdout 2>&1	  		
	)
)

rem Copy files of interest for output
rem AzCopy.exe /Source:%AZ_BATCH_TASK_WORKING_DIR% /Dest:%APSIM_STORAGE_CONTAINER_URL% /DestKey:%APSIM_STORAGE_KEY% /Pattern:%SIMULATION%*
AzCopy.exe /Y /Source:%AZ_BATCH_TASK_WORKING_DIR% /Dest:%APSIM_STORAGE_CONTAINER_URL% /DestKey:%APSIM_STORAGE_KEY% /Pattern:*.out
AzCopy.exe /Y /Source:%AZ_BATCH_TASK_WORKING_DIR% /Dest:%APSIM_STORAGE_CONTAINER_URL% /DestKey:%APSIM_STORAGE_KEY% /Pattern:*.sum
AzCopy.exe /Y /Source:%AZ_BATCH_TASK_WORKING_DIR% /Dest:%APSIM_STORAGE_CONTAINER_URL% /DestKey:%APSIM_STORAGE_KEY% /Pattern:*.stdout
AzCopy.exe /Y /Source:%AZ_BATCH_TASK_WORKING_DIR% /Dest:%APSIM_STORAGE_CONTAINER_URL% /DestKey:%APSIM_STORAGE_KEY% /Pattern:*.db
AzCopy.exe /Y /Source:%MODEL_DIR% /Dest:%APSIM_STORAGE_CONTAINER_URL% /DestKey:%APSIM_STORAGE_KEY% /Pattern:*.db
