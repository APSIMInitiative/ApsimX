@echo off
set APSIM_STORAGE_CONTAINER_URL=https://%AZ_BATCH_ACCOUNT_NAME%.blob.core.windows.net/job-%AZ_BATCH_JOB_ID%-outputs
set JOB_DIR=%AZ_BATCH_NODE_SHARED_DIR%\%AZ_BATCH_JOB_ID% 

echo Starting jobrelease... > %AZ_BATCH_TASK_WORKING_DIR%\jobrelease.stdout 
set >> %AZ_BATCH_TASK_WORKING_DIR%\jobrelease.stdout 

rem Delete folders 
del /q /s %JOB_DIR%\Model >> %AZ_BATCH_TASK_WORKING_DIR%\jobrelease.stdout 
del /q /s %JOB_DIR%\Apsim >> %AZ_BATCH_TASK_WORKING_DIR%\jobrelease.stdout 

AzCopy.exe /Y /Source:%AZ_BATCH_TASK_WORKING_DIR% /Dest:%APSIM_STORAGE_CONTAINER_URL% /DestKey:%APSIM_STORAGE_KEY% /Pattern:*.stdout

del %AZ_BATCH_TASK_WORKING_DIR%\jobrelease.stdout 
