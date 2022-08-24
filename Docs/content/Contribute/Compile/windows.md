---
title: "Windows"
draft: false
---

APSIM can be compiled using Microsoft Visual Studio 2019 or later. A single solution file exists in the root of the repository (ApsimX.sln). All executables will be built to the bin directory. The exact output path will depend upon whether the solution is built in debug or release mode. The default (debug) will output files to `bin\Debug\netcoreapp3.1\`. APSIM currently can only be built against .NET Core 3.1.

Building APSIM requires that version 3.1 of the .NET Core SDK is installed. This can be installed at the same time as Visual Studio. If Visual Studio is already installed, the installation can be modified by downloading and running the [visual studio installer](https://visualstudio.microsoft.com/vs/), and modifying the existing installation by adding the ".NET Core cross-platform development" workload.

![Install the .NET Core SDK](/images/Development.Contribute.Compile.Windows.InstallNetCore.png)

1. Open ApsimX.sln in visual studio
2. Build solution (default ctrl + shift + b)

    Right-click on the solution in the solution explorer and click "Build solution"