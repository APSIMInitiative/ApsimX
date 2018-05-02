---
title: "Linux"
draft: false
---

APSIM can be compiled and run on Linux using MonoDevelop. A single solution file exists in the root of the repository (ApsimX.sln). Building this solution should (in theory) extract all 3rd party packages from NuGet and build everything. All executables will be built to the Bin folder.

I strongly suggest building from a 'clean' source (e.g. not just copying over your ApsimX working directory from Windows). [Version control](/development/contribute/cli/getsource/) is probably the easiest way to do this.

This document provides example commands which work under Ubuntu/Debian. If you are using a different distribution, you will need to use the equivalent command.

1. [Add the mono repository to your system](http://www.mono-project.com/download/stable/#download-lin)

2. Install required packages.

	This is done on Ubuntu via ````sudo apt-get install <PackageName>````
	
	The following packages are required:	
    - gtk-sharp2
	- mono-devel
	- sqlite3
	- libwebkit1.1-cil
	- nuget
	- monodevelop

	To check if a package is already installed, use ````dpkg -s <PackageName>````.
	
	If Gtk# 3 is installed, you will probably need to remove it.
	
3. Update nuget

    Unsure why this is necessary, but it is. 
	
	````sudo nuget update -self````
	
4. Restore nuget packages. MonoDevelop often seems to have trouble doing this automatically, so you need to do it manually.
	
	Navigate to the ApsimX directory and then execute the following command:
	
    ````nuget restore````

	If you get a permissions-related error, run the command again with with root-level access.
	
5. Copy C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\sgen.exe from a Windows machine into ApsimX/Bin/ on your Linux machine.

6. Create a symbolic link in your ApsimX/Bin/ directory called models.exe which points to Models.exe
	
    ````ln -s Models.exe models.exe````
	
	This is necessary because sgen.exe is case-insensitive, so when it is run, it will attempt to execute models.exe (when we actually want it to run Models.exe).

7. Copy ApsimX/ApsimNG/Assemblies/Mono.TextEditor.dll.config to ApsimX/Bin/
	
8. Set ApsimNG as startup project

    In MonoDevelop, right click on ApsimNG, and select "Set as Startup Project".