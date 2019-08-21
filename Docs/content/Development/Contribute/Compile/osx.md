---
title: "macOS"
draft: false
---

APSIM can be compiled using Microsoft's [Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/) 2017 or later. A single solution file exists in the root of the repository (ApsimX.sln). Building this solution will extract all 3rd party packages from NuGet and build everything. All executables will be built to the Bin folder.

1. Install [Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/)

2. Install [git](https://git-scm.com/downloads) or a git GUI client such as [SourceTree](https://www.sourcetreeapp.com/) or [Fork](https://git-fork.com/)

3. [Obtain the source code](../../cli/)

    ```
    git clone https://github.com/APSIMInitiative/ApsimX
    ```

4. Copy the webkit-sharp binary (and its config file) from ApsimX/ApsimNG/Assemblies/ to your ApsimX/Bin/ folder

    ```
    cd /path/to/ApsimX
    cp ApsimNG/Assemblies/webkit-* Bin/
    ```

5. Set ApsimNG as startup project

    ![Set ApsimNG as startup project](/images/macos-apsimng-startup.png)

6. Build and Run