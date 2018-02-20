---
title: "Linux"
draft: false
---

APSIM can be compiled and run on Linux using MonoDevelop. A single solution file exists in the root of the repository (ApsimX.sln). Building this solution should (in theory) extract all 3rd party packages from NuGet and build everything. All executables will be built to the Bin folder.

I strongly suggest building from a 'clean' source (e.g. not just copying over your ApsimX working directory from Windows). [Version control](/development/contribute/cli/getsource/) is probably the easiest way to do this.

0. [Add the mono repository to your system](http://www.mono-project.com/download/stable/#download-lin)

1. Install required packages.

	This is done on Ubuntu via ````sudo apt-get install <PackageName>````
	
	The following packages are required:
	**I think we may actually need mono-complete**
    - gtk-sharp2
	- mono-devel
	- mono-runtime
	- sqlite3
	- libwebkit1.1-cil
	- nuget
	- monodevelop

3. Update nuget

    Unsure why this is necessary, but it is. 
	
	````sudo nuget update -self````
	
4. Restore nuget packages. MonoDevelop often seems to have trouble doing this automatically, so you may need to do it manually:

    ````nuget restore````

	If this fails, go to step 5. Otherwise, proceed to step 6.
	
	
5. Add NuGet sources

	First, you will need write permission on the NuGet config file:
	
	````sudo chmod 666 ~/.config/NuGet/NuGet.Config````
	
	In MonoDevelop, select Edit | Preferences | NuGet | Sources. Select Add from the right-hand panel, and add the following sources:

	OxyPlot Latest: https://www.myget.org/F/oxyplot 

	Official NuGet Gallery: https://www.nuget.org/api/v2/. 

	Once this is done, disable nuget.org: https://api.nuget.org/v3/index.json if it exists.

6. Copy C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\sgen.exe from a Windows machine into ApsimX/Bin/ on your Linux machine.

7. Create a symbolic link in your ApsimX/Bin/ directory called models.exe which points to Models.exe
	
    ````ln -s Models.exe models.exe````
	
	This is necessary because when sgen.exe is run, it is case-insensitive, so it will run models.exe.

