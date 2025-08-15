---
title: "APSIM file versions & APSIM Software versions"
draft: false
---

There are two different versions used in APSIM Next Gen with different meanings and uses. They are:

1. The build number or the APSIM program version (as seen in upgrade). This tracks every time the APSIM Next Generation codebase changes and is packaged for all users (GitHub pull requests with hash resolved) and is used to identify when any fix was made or a new feature added (Git commit history). You report this version in publications for repeatability and transparency as you can ensure the simulation will always give the same results when run with this same version and your simulation file provided with supplementary data, and

2. the three digit version number stored in the apsimx file. This version identifies the "simulation file format version" and tracks changes from (1) that require changes to apsimx simulation settings file (.apsimx json file) for some users, such as where a class or property was renamed, or a new simulation tree structure (model nesting) is implemented. For each increment in this version the developers have added code to automatically update your simulation file to implement the required changes and this will happen automatically when you open a simulation file with an older version number.

Most of the program updates (1) will not require any simulation file changes and so the three-digit version number changes much slower and is usually associated with larger model changes.

APSIM will not allow you to open a simulation with a later simulation file version as this implies the simulation file may have functionality included that is not available in your current version of APSIM.

When you get this error the solutions are:

1. upgrade your APSIM software if you have an installed version.
2. pull the latest changes from APSIM GitHub repo to ensure your development version is up to date.
3. create a copy of your simulation file (apsimx) and edit in a text editor to reduce the version in your apsimx file by:

    * Try to open this file in apsim, and repeat by reducing the version until the error message is not displayed and the simulation opens, or you get an error message about a bad json file in which case you must do (1) or (2) above or know how to address the issue. This assumes the changes required and applied to the apsimx file with the higher version were in parts of the model not used in your simulation and can be ignored.
