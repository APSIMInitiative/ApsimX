######
# Build ApsimX revisions
# Microsoft build tools components: https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-community
# VS Build tools component IDs can be found here: https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-build-tools
######

FROM microsoft/dotnet-framework-build:4.7.1
SHELL ["powershell", "-Command"]

RUN Install-PackageProvider -Name Chocolatey            -Force -RequiredVersion 2.8.5.130;

RUN Set-ExecutionPolicy Bypass -Scope Process -Force; iwr https://chocolatey.org/install.ps1 -UseBasicParsing | iex; 	\
    choco feature disable –name showDownloadProgress;                                        							\
	choco feature enable -n allowGlobalConfirmation;                                         							\
    choco install git;

# Now install some missing build tools
ADD https://aka.ms/vs/15/release/vs_buildtools.exe C:/vs_buildtools.exe

RUN Start-Process -FilePath 'vs_BuildTools.exe' -Wait -ArgumentList 													\
	'--quiet', 																											\
	'--wait', 																											\
	'--norestart', 																										\
	'--nocache', 																										\
	'--installPath C:\BuildTools', 																						\
	'--add Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools'

# compileng.bat is the script which will be run inside the container. It handles cloning/compiling/etc.
COPY compileng.bat compileng.bat

ENTRYPOINT ["compileng.bat"]

RUN rm C:\vs_buildtools.exe;