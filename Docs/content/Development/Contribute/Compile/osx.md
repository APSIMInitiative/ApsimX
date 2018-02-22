---
title: "OSX"
draft: false
---

APSIM can be compiled using Microsoft Visual Studio 2013 or later, MonoDevelop or SharpDevelop. A single solution file exists in the root of the repository (ApsimX.sln). Building this solution will extract all 3rd party packages from NuGet and build everything. All executables will be built to the Bin folder.

1. You need to copy the files from C:\Work\ApsimX\DeploymentSupport\Windows\Bin to your C:\Work\ApsimX\Bin folder
2. You need to add oxyplot package source to NuGet. In Visual Studio, select Tools | NuGet Package Manager | Package Manager Settings menu item. Under the 'Package Sources' item in the tree on the left, add the oxyplot URL: https://www.myget.org/F/oxyplot

![NuGet Package Sources](/images/Development.NuGetPackageSources.png)