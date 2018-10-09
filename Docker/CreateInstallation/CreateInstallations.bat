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

if "%1"=="docs" (
	goto :docs
)

if "%1"=="windows" (
	goto :windows
)
if "%1"=="macos" (
	goto :macos
)
if "%1"=="linux" (
	goto :linux
)

rem User has not provided a valid first argument.
echo Usage: %0 (windows ^| macos ^| linux ^| docs)

:docs
call %apsimx%\Documentation\GenerateDocumentation.bat
exit %errorlevel%

:windows
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
if errorlevel 1 (
	echo Errors encountered!
	exit %errorlevel%
)
rename Output\APSIMSetup.exe APSIMSetup%ISSUE_NUMBER%.exe
echo Uploading APSIMSetup%ISSUE_NUMBER%.exe
@curl -u %APSIM_SITE_CREDS% -T Output\APSIMSetup%ISSUE_NUMBER%.exe ftp://www.apsim.info/APSIM/ApsimXFiles/
exit %errorlevel%

:linux
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
call %setup%\Linux\builddeb.bat
if errorlevel 1 (
	echo Errors encountered!
	exit %errorlevel%
)
if exist %setup%\Output\APSIMSetup.deb (
	rename %setup%\Output\APSIMSetup.deb APSIMSetup%ISSUE_NUMBER%.deb
	echo Uploading APSIMSetup%ISSUE_NUMBER%.deb
	@curl -u %APSIM_SITE_CREDS% -T %setup%\Output\APSIMSetup%ISSUE_NUMBER%.deb ftp://www.apsim.info/APSIM/ApsimXFiles/
) else (
	echo Error - %setup%\Output\APSIMSetup.deb does not exist!
	dir %setup%\Output
	exit 1
)
exit %errorlevel%

:macos
%setup%\osx\BuildMacDist.bat
exit %errorlevel%
