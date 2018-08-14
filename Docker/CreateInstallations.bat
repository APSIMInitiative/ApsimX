@echo off
if not exist APSIM.Shared (
	git clone https://github.com/APSIMInitiative/APSIM.Shared APSIM.Shared
)
git -C APSIM.Shared pull origin master
docker build -m 16g -t createinstallation ApsimX\\Docker\\CreateInstallation
docker run -m 16g --cpu-count %NUMBER_OF_PROCESSORS% --cpu-percent 100 -e APSIM_SITE_CREDS -e NUMBER_OF_PROCESSORS -e ISSUE_NUMBER -v %cd%\ApsimX:C:\ApsimX -v %cd%\APSIM.Shared:C:\APSIM.Shared createinstallation %1
move %cd%\ApsimX\Setup\Output\* %cd%\