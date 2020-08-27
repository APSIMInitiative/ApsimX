---
title: "macOS"
draft: false
---

APSIM can be compiled using Microsoft's [Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/) 2017 or later. A single solution file exists in the root of the repository (ApsimX.sln). Building this solution will extract all 3rd party packages from NuGet and build everything. All executables will be built to the Bin folder.

1. Install [Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/)

2. Install [git](https://git-scm.com/downloads) or a git GUI client such as [SourceTree](https://www.sourcetreeapp.com/) or [Fork](https://git-fork.com/)

3. Install the .NET Core 3.0 SDK. The [dotnet-install script](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script) is a simple way to do this

4. Install GTK+3 and gtksourceview4

    ```
    brew install gtk+3
    brew install gtksourceview4
    ```

5. [Obtain the source code](../../cli/)

    ```
    git clone https://github.com/APSIMInitiative/ApsimX
    ```

6. Copy the webkit-sharp binary (and its config file) from ApsimX/ApsimNG/Assemblies/ to your ApsimX/Bin/ folder

    ```
    cd /path/to/ApsimX
    cp ApsimNG/Assemblies/webkit-* Bin/
    ```

7. Build and Run