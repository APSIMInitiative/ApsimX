---
title: "Windows"
draft: false
---

APSIM can be compiled using Microsoft Visual Studio 2019 or later. A single solution file exists in the root of the repository (ApsimX.sln). All executables will be built to the Bin folder. APSIM can be built against two target frameworks - .NET Framework 4.7.2 (which runs on Linux via mono) and .NET Core 3.0. When building the solution, binaries for both target frameworks will be compiled to the Bin/ and NetCoreBin/ directories, respectively.

Building APSIM requires that the .NET Core SDK is installed. This can be installed at the same time as Visual Studio. If Visual Studio is already installed, the installation can be modified by downloading and running the [visual studio installer](https://visualstudio.microsoft.com/vs/), and modifying the existing installation by adding the ".NET Core cross-platform development" workload.

![Install the .NET Core SDK](/images/Development.Contribute.Compile.Windows.InstallNetCore.png)

1. Open ApsimX.sln in visual studio
2. Build solution (default ctrl + shift + b)

    Right-click on the solution in the solution explorer and click "Build solution"