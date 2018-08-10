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
if not errorlevel 0 (
	exit %errorlevel%
)

rem Microsoft, in their infinite wisdom, decided that it would be a good idea for
rem sysinternals such as sigcheck to spawn a popup window the first time you run them,
rem which asks you to agree to their eula. To get around this, we just need to set a few
rem registry entries...
reg.exe ADD HKCU\Software\Sysinternals /v EulaAccepted /t REG_DWORD /d 1 /f
reg.exe ADD HKU\.DEFAULT\Software\Sysinternals /v EulaAccepted /t REG_DWORD /d 1 /f

sigcheck64 -n -nobanner %apsimx%\Bin\Models.exe > Version.tmp
set /p APSIM_VERSION=<Version.tmp
set issuenumber=%APSIM_VERSION:~-4,4%

rename %setup%\Output\APSIMSetup.exe APSIMSetup%issuenumber%.exe
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
%setup%\Linux\builddeb.bat
if not errorlevel 0 (
	exit %errorlevel%
)
exit %errorlevel%

:macos
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
exit /b %errorlevel%
