---
title: "Linux"
draft: false
---

APSIM can be compiled and run/debugged on Linux using mono/MonoDevelop. A single solution file exists in the root of the repository (ApsimX.sln). All executables will be built to the Bin folder.

1. [Install mono](http://www.mono-project.com/download/stable/#download-lin)

2. Install required packages.

	(These commands and package names are for Ubuntu/Debian.)

	```sudo apt-get install <PackageName>```
	
	The following packages are required:	
    - gtk-sharp2
	- mono-devel
	- sqlite3
	- libwebkit1.1-cil
	- nuget
	- monodevelop
	- git (required for obtaining the source code)

	To check if a package is already installed, use ```dpkg -s <PackageName>```

3. Obtain source code

	```git clone https://github.com/APSIMInitiative/ApsimX```

3. Ensure nuget is up-to-date

	````sudo nuget update -self````

4. Restore nuget packages

    ````nuget restore````

5. Copy ApsimX/ApsimNG/Assemblies/Mono.TextEditor.dll.config to ApsimX/Bin/

6. Copy ApsimX/ApsimNG/Assemblies/webkit-sharp.dll to ApsimX/Bin/

	This is not necessary if your distribution includes a package for webkit-sharp.

7. Set ApsimNG as startup project

    In MonoDevelop, right click on ApsimNG, and select "Set as Startup Project".
