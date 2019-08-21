---
title: "Linux"
draft: false
---

APSIM can be compiled and run on Linux using MonoDevelop. A single solution file exists in the root of the repository (ApsimX.sln). Building this solution should extract all 3rd party packages from NuGet and build everything. All executables will be built to the Bin folder.

I strongly suggest building from a 'clean' source (e.g. not just copying over your ApsimX working directory from Windows). [Version control](/development/contribute/cli/getsource/) is probably the easiest way to do this.

This document provides example commands which work under Ubuntu/Debian. If you are using a different distribution, you will need to use the equivalent command for your package manager. Package names also may be slightly different in other distributions.

1. [Add the mono repository to your system](http://www.mono-project.com/download/stable/#download-lin)

2. Install required packages.

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

3. Update nuget

	````sudo nuget update -self````

4. Restore nuget packages. MonoDevelop often seems to have trouble doing this automatically, so you need to do it manually.

	Navigate to the ApsimX directory and then execute the following command:

    ````nuget restore````

5. Copy ApsimX/ApsimNG/Assemblies/Mono.TextEditor.dll.config to ApsimX/Bin/

6. Set ApsimNG as startup project

    In MonoDevelop, right click on ApsimNG, and select "Set as Startup Project".
