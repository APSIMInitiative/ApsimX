---
title: "MacOS Installation"
draft: false
---
Last Updated 4/4/2024
Please alert us at our [github page](https://github.com/APSIMInitiative/ApsimX/issues) if these instructions become outdated or incorrect.

Be aware that these instructions will require administrator privileges to be completed.

Apsim requires four libraries to be installed to run on MacOS:

1. [.NET version 6.0] (https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
2. [Homebrew] (https://brew.sh/)
3. [Gtk+3](https://docs.gtk.org/gtk3/macos.html) (Using Homebrew)
4. [GtkSourceView4](https://github.com/GNOME/gtksourceview) (Using Homebrew)

# Installing .NET
Apsim requires the **x64 verion of the .NET 6.0 SDK library**. At time of writing the latest version of .NET is 8.0, so be very careful which version you are installing.

Do **NOT** install the Arm64 version. The x64 version will work on an Arm M1/M2/M3 system, and is the library that Apsim is looking to run with.

![.NET Version](/images/netversion.png)

Download the SDK MacOS x64 library and run the installer it gives you. Afterwards, open up a terminal and run:

```
dotnet --list-sdks
```

It should respond with the version of .NET that you have installed. If it does not, you may need to link .NET manually with this terminal command:

```
sudo ln -s /usr/local/share/dotnet/x64/dotnet /usr/local/bin/
```

Once you can get the dotnet versions showing in the terminal, dotnet should be correctly installed:

```
user@system ~ % dotnet --list-sdks
6.0.420 [/usr/local/share/dotnet/sdk]
```

# Installing Homebrew
Use the console to install Homebrew if you don't have it installed aready.

```
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

Afterwards, check the version with:

```
brew --version
```

If brew cannot be found, you may need to link it manually with:

```
echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> ~/.zprofile
eval "$(/opt/homebrew/bin/brew shellenv)"
```

Once you can get the brew version showing, it is correctly installed:

```
user@system ~ % brew --version
Homebrew 3.2.6
Homebrew/homebrew-core (git revision a97c2a6737; last commit 2021-08-09)
Homebrew/homebrew-cask (git revision 8e931b20db; last commit 2021-08-09)
```

# Installing Gtk3 and GtkSourceView
With Homebrew installed, you then need to install gtk and the gtk source view libraries. These are installed with:

GTK3:
```
brew install gtk+3
```

GTK SourceView 4:
```
brew install gtksourceview4
```

# Installing Apsim

1. To install Apsim on MacOS, download a copy from the [apsim website](https://www.apsim.info/download-apsim/).
2. Click 'Click here to download or upgrade APSIM' which will take you to a registration website.
3. Next, enter your email address
4. On the next screen, in the 'Download Link' column, select 'MacOS'. This will download an apsim dmg file.
5. Open the dmg and drag the app to your Applications folder
6. Run the Apsim file you just created in Aplications
7. **A prompt will say that "APSIM cannot be opened because it is from an unidentified developer". Close this Window.**
8. Run the application again and it will now have an Open option, click that.

# Common issues
**Unable to open APSIM after install.**
This usually means that a library is missing from your system that Apsim requires. Check that you have installed the correct version of .NET and that you have both GTK3 and GTKSourceView4 installed.

**Running Apsim from the terminal**
If Apsim is not opening from the Applications folder, open a terminal and type the following with the version number you installed:

```
open ../../Applications/APSIM2024.4.7437.0.app
```

If Apsim does not open, this will give an error message that could help you work out what is causing it to fail. If you report an issue with running Apsim on MacOS, please provide these error messages to help us solve your problem.

**SQLite Permissions Error**
There are a number of reasons why the database may have permission problems.

1. If your apsim file is within a Dropbox/OneDrive/Cloud Storage folder, that will cause problems when accessing the database due to the cloud storage trying to sync the file while it's being changed.
2. It has been reported that running apsim from the terminal can fix this error if it's not related to cloud storage.