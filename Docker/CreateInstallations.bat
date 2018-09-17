@echo off
if "%1"=="macos" (
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
	docker build -t createosxinstallation ApsimX\Docker\osx
	docker run -e APSIM_SITE_CREDS -e ISSUE_NUMBER -v "%cd%\ApsimX":/ApsimX createosxinstallation
) else (
	docker build -m 16g -t createinstallation %~dp0CreateInstallation
	docker run -m 16g --cpu-count %NUMBER_OF_PROCESSORS% --cpu-percent 100 -e APSIM_SITE_CREDS -e NUMBER_OF_PROCESSORS -e ISSUE_NUMBER -v %~dp0..:C:\ApsimX createinstallation %1
)
