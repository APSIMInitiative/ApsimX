@echo off
for /D %%D in (%%~dp0\..\Tests\Validation\*) do (
    echo %%~nD
	DIR %%D
    set ModelName=%%~nD%%~xD
	set ModelName
)