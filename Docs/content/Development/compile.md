---
title: "How to compile"
draft: false
weight: 20
---

# Windows

APSIM can be compiled using Microsoft Visual Studio 2019 or later. A single solution file exists in the root of the repository (ApsimX.sln). All executables will be built to the bin directory. The exact output path will depend upon whether the solution is built in debug or release mode. The default (debug) will output files to `bin\Debug\net8.0\`. APSIM currently can only be built against .NET 8.

Building APSIM requires that version 8.0 of the .NET SDK is installed. This can be installed at the same time as Visual Studio. If Visual Studio is already installed, the installation can be modified by navigating to 'Tools\Get Tools and Features...' in the menu bar and modifying the existing installation by adding either the ".NET desktop development" or "Universal Windows Platform development" workload.

![Install the .NET Core SDK](/images/vs-modify-workload.png)

1. Open ApsimX.sln in visual studio
2. Build solution (default ctrl + shift + b)

    Right-click on the solution in the solution explorer and click "Build solution"

# LINUX


Apsim may be built with the .NET SDK - currently version 8.0 is required. When building the solution, assemblies for all projects will be compiled to the bin/ directory. The exact location of a given file will depend upon how it is built - e.g. debug vs release configuration.

1. Install the .NET Core 8.0 SDK. The [dotnet-install script](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script) is a simple way to do this. Otherwise, consult [this page](https://docs.microsoft.com/en-us/dotnet/core/install/linux)

2. Install required packages

	- libsqlite3-dev
	- git (required for obtaining the source code)
    - gtk-sharp3
	- libgtksourceview-4-0
	- dotnet-sdk-8.0

3. Obtain source code

	```
	git clone https://github.com/APSIMInitiative/ApsimX
	```

4. Build the solution

	```bash
	dotnet build ApsimX.sln            # (debug mode)
	dotnet build -c Debug ApsimX.sln   # (debug mode)
	dotnet build -c Release ApsimX.sln # (release mode)
	```

5. Run apsim

    The outputs may be found under ApsimX/bin. If built in debug mode, they will be in `bin/Debug/net8.0/`. If built in release mode, they will be in `bin/Release/net8.0/`.

	The entrypoint program for the user interface is called `ApsimNG`.

	The CLI has two "main" entrypoints - `Models` and `apsim`. `Models` may be used to run .apsimx files. `apsim` accepts multiple verb arguments, but `apsim run` will function identically to an invocation of `Models`.

## Common Problems

When running apsim:

```
System.DllNotFoundException: Unable to load shared library 'sqlite3' or one of its dependencies. In order to help diagnose loading problems, consider setting the LD_DEBUG environment variable: libsqlite3: cannot open shared object file: No such file or directory
```

This error can occur on Debian (and its derivatives) when the sqlite3 package is installed. This package typically provides a file named `libsqlite3.so.0` or similar, but apsim is looking for `libsqlite3.so`. This file is provided by the `libsqlite3-dev` package, so installing this package should fix the problem. Otherwise, creating a symlink to `libsqlite3.so.0` called `libsqlite3.so` should also fix the problem.

---

When attempting to install apsim from our binary package:

```
E: Unable to locate package dotnet-runtime-8.0
```

This package is not included in the official Debian repositories. You will need to follow the instructions on [this page](https://docs.microsoft.com/en-us/dotnet/core/install/linux) to install the package from microsoft's repositories.

# Mac OSX


APSIM can be compiled using Microsoft's [Visual Studio Code](https://code.visualstudio.com/download). A single solution file exists in the root of the repository (ApsimX.sln). Building this solution will extract all 3rd party packages from NuGet and build everything. All executables will be built to the bin folder, but the exact output location will depend on how the solution is built (ie release vs debug). The default (debug) will cause outputs to be copied to `bin/Debug/net8.0/`.

1. Install [Visual Studio Code](https://code.visualstudio.com/download)

2. Once visual studio is installed, install the vs code c# dev kit extension.

3. Install [git](https://git-scm.com/downloads) and a git client, we recommend [Fork](https://git-fork.com/).

4. Install the .NET 8 SDK. The SDK can be found [here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) for your specific operating system.

5. Check that the SDK is installed by opening a terminal and running the command:

    ```
    dotnet --list-sdks
    ```
    - You should see at least one line that says:

        ```
        8.X.X
        ```
        - `x` is any number. The version numbers of a SDK may change over time.
        - There may be other lines with differing values. This is normal.


5. Install GTK+3 and gtksourceview4

    ```
    brew install gtk+3
    brew install gtksourceview4
    ```

6. Obtain the source code

    ```
    git clone https://github.com/APSIMInitiative/ApsimX
    ```

7. Build and Run