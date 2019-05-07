@echo off

rem Install chocolatey.
@"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))" && SET "PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin"

call refreshenv

rem Disable the chocolatey progress bar. This doesn't display well in a log file.
choco feature disable –name showDownloadProgress
choco feature enable -n allowGlobalConfirmation
rem curl is used to call several web services as well as to upload the installers (for now).
rem wget is used to download utilities unavailable via chocolatey.
rem git is needed for obvious reasons.
rem innosetup is the tool which generates the Windows installer.
rem fciv is used when creating the Debian installer.
rem cygwin provides several useful utilities such as genisoimage
rem 7zip will be needed to extract dependencies which we are about to download.
rem R is needed to run Apsim's sensitivity analysis.
rem NUnit is needed to run the unit tests.
choco install curl wget git innosetup fciv cygwin 7zip r.project nunit-console-runner

mkdir C:\Utilities>nul
mkdir C:\Utilities\downloads>nul
pushd C:\Utilities\downloads>nul

rem Now install some missing build tools
echo Downloading Visual Studio installer...
wget -q --no-check-certificate https://aka.ms/vs/15/release/vs_buildtools.exe -O vs_buildtools.exe
echo Installing Visual Studio build tools...

vs_BuildTools.exe -q --wait --norestart --nocache --installPath C:\Utilities\BuildTools --add Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools --add Microsoft.Net.Component.4.6.TargetingPack --add Microsoft.Net.Component.4.5.2.TargetingPack
call C:\Utilities\BuildTools\Common7\Tools\VsDevCmd.bat

rem Sigcheck is used several times to get the Apsim version number from Models.exe.
echo Downloading sigcheck...
wget -q --no-check-certificate https://download.sysinternals.com/files/Sigcheck.zip -O sigcheck.zip
echo Installing sigcheck...
7z e sigcheck.zip *.exe -o..>nul

rem dos2unix is to convert a few files' crlf to lf when creating the Debian/macos installers.
echo Downloading dos2unix...
wget -q --no-check-certificate https://nchc.dl.sourceforge.net/project/dos2unix/dos2unix/7.4.0/dos2unix-7.4.0-win64.zip -O dos2unix.zip
echo Installing dos2unix...
7z e dos2unix.zip bin\dos2unix.exe -o..>nul

rem ar is used to create the Debian installer.
echo Downloading ar...
wget -q http://bob.apsim.info/files/ar.exe -O ..\ar.exe
echo Installing ar...

rem coreutils. This contains the md5sum utility, which is used when creating the Debian installer.
echo Downloading coreutils...
wget -q --no-check-certificate https://nchc.dl.sourceforge.net/project/gnuwin32/coreutils/5.3.0/coreutils-5.3.0-bin.zip -O coreutils_bin.zip
echo Installing coreutils...
7z e coreutils_bin.zip bin\du.exe bin\md5sum.exe -o..>nul

rem Dependencies for coreutils.
echo Downloading coreutils dependencies...
wget -q --no-check-certificate https://nchc.dl.sourceforge.net/project/gnuwin32/coreutils/5.3.0/coreutils-5.3.0-dep.zip -O coreutils_dep.zip
echo Installing coreutils dependencies
7z e coreutils_dep.zip bin\*.dll -o..>nul

rem tar is used when creating the Debian installer.
echo Downloading tar...
wget -q --no-check-certificate https://nchc.dl.sourceforge.net/project/gnuwin32/tar/1.13-1/tar-1.13-1-bin.zip -O tar_bin.zip
echo Installing tar...
7z e tar_bin.zip bin\tar.exe -o..>nul

rem Dependencies for tar.
echo Downloading tar dependencies...
wget -q --no-check-certificate https://nchc.dl.sourceforge.net/project/gnuwin32/tar/1.13-1/tar-1.13-1-dep.zip -O tar_dep.zip
echo Installing tar dependencies...
7z e tar_dep.zip bin\*.dll -o..>nul

rem # tar -z option doesn't seem to work on windows (What's the point of gnuwin32 if such a fundamental feature doesn't work?!?!)
rem # so we will need to install gzip as well
echo Downloading gzip...
wget -q --no-check-certificate https://nchc.dl.sourceforge.net/project/gnuwin32/gzip/1.3.12-1/gzip-1.3.12-1-bin.zip -O gzip.zip
echo Installing gzip...
7z e gzip.zip bin\gzip.exe -o..>nul

rem genisoimage is used to create the MacOS installer.
echo Downloading genisoimage...
wget -q http://bob.apsim.info/files/genisoimage.exe -O ..\genisoimage.exe
echo Installing genisoimage...

echo Downloading NuGet...
wget -q --no-check-certificate https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -O ..\nuget.exe
echo Installing NuGet...

echo Performing additional setup...

cd C:\Utilities
rmdir /s /q downloads

rem Microsoft, in their infinite wisdom, decided that it would be a good idea for
rem sysinternals such as sigcheck to spawn a popup window the first time you run them,
rem which asks you to agree to their eula. To get around this, we just need to set a few
rem registry entries...
reg.exe ADD HKCU\Software\Sysinternals /v EulaAccepted /t REG_DWORD /d 1 /f
reg.exe ADD HKU\.DEFAULT\Software\Sysinternals /v EulaAccepted /t REG_DWORD /d 1 /f

rem Modify registry entry so that DateTime format is dd/MM/yyyy.
echo Modifying system DateTime format...
reg add "HKCU\Control Panel\International" /v sShortDate /d "dd/MM/yyyy" /f

setx PATH "%PATH%;C:\Utilities;C:\Utilities\BuildTools\MSBuild\15.0\Bin"
echo Done!
popd>nul
pause