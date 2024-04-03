---
title: "MacOS Installation"
draft: false
---

1. To install Apsim on MacOS, download a copy from the [apsim website](https://www.apsim.info/download-apsim/).
2. Click 'Click here to download or upgrade APSIM' which will take you to a registration website.
3. Next, enter your email address
4. On the next screen, in the 'Download Link' column, select 'MacOS'. This will download an apsim dmg file.
5. Once this has finished downloading, double click the apsim icon in the MacOS dock.
6. A prompt will say that "APSIMXXXX.X.XXXX.X" cannot be opened because it is from an unidentified developer.
7. Open the 'Finder' application, locate the APSIM app in the 'Applications' folder. 
8. If the application is not located here, move the application to the applications folder. Hold control and click APSIM app and click 'open'. You should no longer see a prompt from now on when opening the APSIM app.

# Common issues
## Unable to open APSIM after install.
- This usually means that .NET 6.0 is not installed on your system. You can install the .NET 6.0 SDK which should resolve the issue. You can get the .NET 6.0 SDK [here](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

## Unable to see the user interface
- This usually means GTK+3 and/or gtksourceview4 are not installed. These are the libraries that handle the user interface for APSIM and can be downloaded using [homebrew](https://brew.sh/)
- Once you have homebrew installed, run the commands: `brew install gtk+3` and `brew install gtksourceview4`.
- Retry running APSIM.