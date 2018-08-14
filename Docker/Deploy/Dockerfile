FROM microsoft/windowsservercore:10.0.14393.206
SHELL ["powershell", "-Command"]


RUN Install-PackageProvider -Name Chocolatey            -Force -RequiredVersion 2.8.5.130;

RUN Set-ExecutionPolicy Bypass -Scope Process -Force; iwr https://chocolatey.org/install.ps1 -UseBasicParsing | iex; 	\
    choco feature disable –name showDownloadProgress;                                        							\
	choco feature enable -n allowGlobalConfirmation;                                         							\
	choco install curl;

# run.bat is the script which will be run inside the container.
COPY deploy.bat deploy.bat

ENTRYPOINT ["deploy.bat"]