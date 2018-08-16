################################################
# Docker image to create ApsimX installations. #
################################################

FROM microsoft/windowsservercore:10.0.14393.206
SHELL ["powershell", "-Command"]

# dos2unix
ADD https://nchc.dl.sourceforge.net/project/dos2unix/dos2unix/7.4.0/dos2unix-7.4.0-win64.zip /dos2unix.zip

# sigcheck, used to check signatures of certain files
ADD https://download.sysinternals.com/files/Sigcheck.zip /sigcheck.zip

# ar, the archive manager
ADD http://bob.apsim.info/files/ar.exe /ar.exe

# coreutils. This contains the md5sum utility.
ADD https://nchc.dl.sourceforge.net/project/gnuwin32/coreutils/5.3.0/coreutils-5.3.0-bin.zip /coreutils_bin.zip

# coreutils dependencies
ADD https://nchc.dl.sourceforge.net/project/gnuwin32/coreutils/5.3.0/coreutils-5.3.0-dep.zip /coreutils.zip

# tar dependencies
ADD https://nchc.dl.sourceforge.net/project/gnuwin32/tar/1.13-1/tar-1.13-1-dep.zip /tar_dependencies.zip

# tar
ADD https://nchc.dl.sourceforge.net/project/gnuwin32/tar/1.13-1/tar-1.13-1-bin.zip /tar.zip

# tar -z option doesn't seem to work on windows (What's the point of gnuwin32 if such a fundamental feature doesn't work?!?!)
# so we will need to install gzip as well
ADD https://nchc.dl.sourceforge.net/project/gnuwin32/gzip/1.3.12-1/gzip-1.3.12-1-bin.zip /gzip.zip

RUN Install-PackageProvider -Name Chocolatey -Force -RequiredVersion 2.8.5.130;											\
	Set-ExecutionPolicy Bypass -Scope Process -Force; iwr https://chocolatey.org/install.ps1 -UseBasicParsing | iex; 	\
    choco feature disable –name showDownloadProgress;                                        							\
	choco feature enable -n allowGlobalConfirmation;                                         							\
    choco install --limit-output git																					\
	choco install --limit-output innosetup;																				\
	choco install --limit-output fciv;																					\
	choco install --limit-output cygwin;																				\
	choco install --limit-output 7zip;																					\
	choco install --limit-output curl;																					\
	mkdir C:\Utilities;																									\
	Expand-Archive -Path C:\coreutils.zip -DestinationPath C:\coreutils -Force;											\
	cp C:\coreutils\bin\* C:\Utilities\;																				\
	Expand-Archive -Path C:\coreutils_bin.zip -DestinationPath C:\coreutils_bin -Force;									\
	cp C:\coreutils_bin\bin\* C:\Utilities\;																			\
	Expand-Archive -Path C:\dos2unix.zip -DestinationPath C:\dos2unix -Force;											\
	cp C:\dos2unix\bin\* C:\Utilities\;																					\
	Expand-Archive -Path C:\sigcheck.zip -DestinationPath C:\sigcheck -Force;											\
	cp C:\sigcheck\*.exe C:\Utilities\;																					\
	Expand-Archive -Path C:\tar.zip -DestinationPath C:\tar -Force;														\
	cp C:\tar\bin\*.exe C:\Utilities\;																					\
	Expand-Archive -Path C:\tar_dependencies.zip -DestinationPath C:\tar_dependencies -Force;							\
	cp C:\tar_dependencies\bin\* C:\Utilities\;																			\
	Expand-Archive -Path C:\gzip.zip -DestinationPath C:\gzip -Force;													\
	cp C:\gzip\bin\* C:\Utilities\;																						\
	cp C:\ar.exe C:\Utilities\ar.exe;

# run.bat is the script which will be run inside the container.
COPY CreateInstallations.bat CreateInstallations.bat

ENTRYPOINT ["CreateInstallations.bat"]