@echo off

set apsimx=C:\ApsimX
if not exist %apsimx% (
	echo Error: C:\ApsimX does not exist.
	exit 1
)

if exist %apsimx%\bin.zip (
	echo Unzipping %apsimx%\bin.zip...
	powershell -Command Expand-Archive -Path %apsimx%\bin.zip -DestinationPath %apsimx%\Bin -Force
	if %errorlevel% neq 0 (
		echo Error unzipping %apsimx%\bin.zip
		exit %errorlevel%
	)
)

set bin=%apsimx%\Bin
if not exist %bin% (
	echo %bin% does not exist. Aborting...
	exit 1
)

set setup=%apsimx%\Setup
echo ########### Creating documentation
cd %apsimx%\Documentation
call GenerateDocumentation.bat

echo.
echo.
echo   _____                _   _              __          ___           _                     _____           _        _ _       _   _             
echo  / ____^|              ^| ^| ^|_)             \ \        / ^|_)         ^| ^|                   ^|_   _^|         ^| ^|      ^| ^| ^|     ^| ^| ^|_)            
echo ^| ^|     _ __ ___  __ _^| ^|_ _ _ __   __ _   \ \  /\  / / _ _ __   __^| ^| _____      _____    ^| ^|  _ __  ___^| ^|_ __ _^| ^| ^| __ _^| ^|_ _  ___  _ __  
echo ^| ^|    ^| '__/ _ \/ _` ^| __^| ^| '_ \ / _` ^|   \ \/  \/ / ^| ^| '_ \ / _` ^|/ _ \ \ /\ / / __^|   ^| ^| ^| '_ \/ __^| __/ _` ^| ^| ^|/ _` ^| __^| ^|/ _ \^| '_ \ 
echo ^| ^|____^| ^| ^|  __/ ^|_^| ^| ^|_^| ^| ^| ^| ^| ^|_^| ^|    \  /\  /  ^| ^| ^| ^| ^| ^|_^| ^| ^|_) \ V  V /\__ \  _^| ^|_^| ^| ^| \__ \ ^|^| ^|_^| ^| ^| ^| ^|_^| ^| ^|_^| ^| ^|_) ^| ^| ^| ^|
echo  \_____^|_^|  \___^|\__,_^|\__^|_^|_^| ^|_^|\__, ^|     \/  \/   ^|_^|_^| ^|_^|\__,_^|\___/ \_/\_/ ^|___/ ^|_____^|_^| ^|_^|___/\__\__,_^|_^|_^|\__,_^|\__^|_^|\___/^|_^| ^|_^|
echo                                     __/ ^|                                                                                                      
echo                                    ^|___/                                                                                                       
echo.
echo.
cd %setup%
ISCC.exe apsimx.iss

echo.
echo.
echo   _____                _   _               _____       _     _               _____           _                    
echo  / ____^|              ^| ^| ^(_^)             ^|  __ \     ^| ^|   ^(_^)             ^|  __ \         ^| ^|                   
echo ^| ^|     _ __ ___  __ _^| ^|_ _ _ __   __ _  ^| ^|  ^| ^| ___^| ^|__  _  __ _ _ __   ^| ^|__^) ^|_ _  ___^| ^| ____ _  __ _  ___ 
echo ^| ^|    ^| '__/ _ \/ _` ^| __^| ^| '_ \ / _` ^| ^| ^|  ^| ^|/ _ \ '_ \^| ^|/ _` ^| '_ \  ^|  ___/ _` ^|/ __^| ^|/ / _` ^|/ _` ^|/ _ \
echo ^| ^|____^| ^| ^|  __/ ^(_^| ^| ^|_^| ^| ^| ^| ^| ^(_^| ^| ^| ^|__^| ^|  __/ ^|_^) ^| ^| ^(_^| ^| ^| ^| ^| ^| ^|  ^| ^(_^| ^| ^(__^|   ^< ^(_^| ^| ^(_^| ^|  __/
echo  \_____^|_^|  \___^|\__^,_^|\__^|_^|_^| ^|_^|\__^, ^| ^|_____/ \___^|_.__/^|_^|\__^,_^|_^| ^|_^| ^|_^|   \__^,_^|\___^|_^|\_\__^,_^|\__^, ^|\___^|
echo                                     __/ ^|                                                               __/ ^|     
echo                                    ^|___/                                                               ^|___/      
echo.
echo.
cd %setup%\Linux
call BuildDeb.bat

echo.
echo.
echo   _____                _   _               __  __             ____   _____   _____           _        _ _       _   _             
echo  / ____^|              ^| ^| ^(_^)             ^|  \/  ^|           / __ \ / ____^| ^|_   _^|         ^| ^|      ^| ^| ^|     ^| ^| ^(_^)            
echo ^| ^|     _ __ ___  __ _^| ^|_ _ _ __   __ _  ^| \  / ^| __ _  ___^| ^|  ^| ^| ^(___     ^| ^|  _ __  ___^| ^|_ __ _^| ^| ^| __ _^| ^|_ _  ___  _ __  
echo ^| ^|    ^| '__/ _ \/ _` ^| __^| ^| '_ \ / _` ^| ^| ^|\/^| ^|/ _` ^|/ __^| ^|  ^| ^|\___ \    ^| ^| ^| '_ \/ __^| __/ _` ^| ^| ^|/ _` ^| __^| ^|/ _ \^| '_ \ 
echo ^| ^|____^| ^| ^|  __/ ^(_^| ^| ^|_^| ^| ^| ^| ^| ^(_^| ^| ^| ^|  ^| ^| ^(_^| ^| ^(__^| ^|__^| ^|____^) ^|  _^| ^|_^| ^| ^| \__ \ ^|^| ^(_^| ^| ^| ^| ^(_^| ^| ^|_^| ^| ^(_^) ^| ^| ^| ^|
echo  \_____^|_^|  \___^|\__,_^|\__^|_^|_^| ^|_^|\__, ^| ^|_^|  ^|_^|\__,_^|\___^|\____/^|_____/  ^|_____^|_^| ^|_^|___/\__\__,_^|_^|_^|\__,_^|\__^|_^|\___/^|_^| ^|_^|
echo                                     __/ ^|                                                                                         
echo                                    ^|___/                                                                                          
echo.
echo.
cd "%setup%\OS X"
call BuildMacDist.bat



rem echo ########### Uploading installations
rem cd "C:\Jenkins\workspace\1. GitHub pull request\ApsimX\Setup"
rem call C:\Jenkins\ftpcommand.bat %Issue_Number%
rem echo.
rem 
rem echo ########### Add a green build to DB
rem set /p PASSWORD=<C:\Jenkins\ChangeDBPassword.txt
rem curl -k https://www.apsim.info/APSIM.Builds.Service/Builds.svc/AddGreenBuild?pullRequestNumber=%ghprbPullId%^&buildTimeStamp=%DATETIMESTAMP%^&changeDBPassword=%PASSWORD%
