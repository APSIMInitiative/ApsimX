@echo off
rem Microsoft, in their infinite wisdom, decided that it would be a good idea for
rem sysinternals such as sigcheck to spawn a popup window the first time you run them,
rem which asks you to agree to their eula. To get around this, we just need to set a few
rem registry entries...
reg.exe ADD HKCU\Software\Sysinternals /v EulaAccepted /t REG_DWORD /d 1 /f
reg.exe ADD HKU\.DEFAULT\Software\Sysinternals /v EulaAccepted /t REG_DWORD /d 1 /f