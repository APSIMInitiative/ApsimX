################################################
# Docker image to create ApsimX installations. #
################################################

FROM microsoft/windowsservercore:10.0.14393.206
SHELL ["powershell", "-Command"]

ADD https://sourceforge.net/projects/dos2unix/files/dos2unix/7.4.0/dos2unix-7.4.0-win64.zip/download /dos2unix.zip

ADD https://download.sysinternals.com/files/Sigcheck.zip /sigcheck.zip

ADD https://download.sysinternals.com/files/DU.zip /du.zip

RUN Install-PackageProvider -Name Chocolatey -Force -RequiredVersion 2.8.5.130;											\
	Set-ExecutionPolicy Bypass -Scope Process -Force; iwr https://chocolatey.org/install.ps1 -UseBasicParsing | iex; 	\
    choco feature disable –name showDownloadProgress;                                        							\
	choco feature enable -n allowGlobalConfirmation;                                         							\
    choco install git;                                                                                                  \
	choco install innosetup;																							\
	choco install fciv;																									\
	Expand-Archive -Path C:\dos2unix.zip -DestinationPath C:\dos2unix -Force;											\
	mkdir C:\Utilities;																									\
	cp C:\dos2unix\bin\* C:\Utilities\;																					\
	Expand-Archive -Path C:\sigcheck.zip -DestinationPath C:\sigcheck -Force;											\
	cp C:\sigcheck\*.exe C:\Utilities\;																					\
	Expand-Archive -Path C:\du.zip -DestinationPath C:\du -Force;														\
	cp C:\du\*.exe C:\Utilities\;

# run.bat is the script which will be run inside the container.
COPY CreateInstallations.bat CreateInstallations.bat

#ENTRYPOINT ["CreateInstallations.bat"]