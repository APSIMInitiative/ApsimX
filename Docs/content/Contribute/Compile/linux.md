---
title: "Linux"
draft: false
---

APSIM can be built against two target frameworks - .NET Framework 4.7.2 (which runs on Linux via mono) and .NET Core 3.1. When building the solution, binaries for both target frameworks will be compiled to the bin/ directory.

1. [Install mono](http://www.mono-project.com/download/stable/#download-lin)

2. Install the .NET Core 3.1 SDK. The [dotnet-install script](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script) is a simple way to do this. Otherwise, consult [this page](https://docs.microsoft.com/en-us/dotnet/core/install/linux)

3. Install required packages

	```apt install <PackageName>```

	The two target frameworks have slightly different sets of dependencies. When building the solution file, binaries are built for *both* target frameworks by default, so all dependencies will be required. The following packages are required for both .NET Framework and .NET Core builds:

	- libsqlite3-dev
	- git (required for obtaining the source code)

	For .NET Framework builds:

	- mono-devel
	- gtk-sharp2
	- libwebkit1.1-cil
	    
		Note: this package is no longer available in Ubuntu's official repositories. It can be built from source, but can also be considered an optional dependency. The user interface will run without it, but some parts will be missing.

	- webkit-sharp
	
		Note: this package is no longer available in Ubuntu's official repositories. If unavailable, run this command from the ApsimX directory:
		
		```cp ApsimNG/Assemblies/webkit-sharp.dll Bin/```

	For .NET Core builds:

    - gtk-sharp3
	- libgtksourceview-4-0
	- dotnet-sdk-3.1

	To check if a package is already installed, use ```dpkg -s <PackageName>```

4. Obtain source code

	```git clone https://github.com/APSIMInitiative/ApsimX```

5. Copy ApsimX/ApsimNG/Assemblies/Mono.TextEditor.dll.config to ApsimX/Bin/

6. Build the solution

	```dotnet build ApsimX.sln```

	The `-f` switch may be used to build for a specific target framework. E.g.

	```dotnet build -f netcoreapp3.0 ApsimX.sln```

	or

	```dotnet build -f net472 ApsimX.sln```

## Common Problems

Attempts to build the .net framework version of apsim can sometimes result in this error:

```
/usr/share/dotnet/sdk/5.0.202/Microsoft.Common.CurrentVersion.targets(1216,5): error MSB3644: The reference assemblies for .NETFramework,Version=v4.6.1 were not found. To resolve this, install the Developer Pack (SDK/Targeting Pack) for this framework version or retarget your application. You can download .NET Framework Developer Packs at https://aka.ms/msbuild/developerpacks
```

This can be fixed by setting the FrameworkPathOverride environment variable. E.g.

```bash
FrameworkPathOverride=/usr/lib/mono/4.7.2-api/ dotnet build ApsimX.sln
```

---

When running apsim:

```
System.DllNotFoundException: Unable to load shared library 'sqlite3' or one of its dependencies. In order to help diagnose loading problems, consider setting the LD_DEBUG environment variable: libsqlite3: cannot open shared object file: No such file or directory
```

This error can occur on Debian (and its derivatives) when the sqlite3 package is installed. This package typically provides a file named `libsqlite3.so.0` or similar, but apsim is looking for `libsqlite3.so`. This file is provided by the `libsqlite3-dev` package, so installing this package should fix the problem. Otherwise, creating a symlink to `libsqlite3.so.0` called `libsqlite3.so` should also fix the problem.

---

When attempting to install apsim from our binary package:

```
E: Unable to locate package dotnet-runtime-3.1
```

This package is not included in the official Debian repositories. You will need to follow the instructions on [this page](https://docs.microsoft.com/en-us/dotnet/core/install/linux) to install the package from microsoft's repositories.
