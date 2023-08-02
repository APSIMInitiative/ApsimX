---
title: "1. Get source code from GitHub"
draft: true
---

## Create GitHub account

The APSIM source code is located on GitHub so you will need to have a GitHub login if you want to contribute new features or modifications to the APSIM repository.

This document assumes that your APSIM Next Generation folder is C:\Work\ApsimX.

## Fork the repository

If you plan to modify or add to the APSIM code/datasets, you will need to fork the repository. A fork creates a copy of the repository associated with your github account.

* Click on the fork link in the top right hand corner of the [APSIM repository](https://github.com/APSIMInitiative/ApsimX) on GitHub

## Clone ApsimX to your computer

To bring the source code from GitHub to your computer, you will need to clone the repository. We recommend you use a Git GUI client. We recommend [SourceTree](http://www.sourcetreeapp.com) for this. Once you have SourceTree installed on your computer, goto *File | Clone* menu item.

Click the 'Clone / New' button in SourceTree and specify:

* Source Path / URL: https://github.com/APSIMInitiative/ApsimX
* Destination Path: C:\Work\ApsimX   <- this is the folder on your computer 
* Name: ApsimX   <- This is the name that this respository will be known by

Once you click 'Clone', all files will be downloaded to your computer into the destination path that you specified above. SourceTree will create a tab for your new cloned repository.

## Add a remote repository

You now need to create a link to your ApsimX fork that you created earlier.

* Click Repository | Repository Settings
* Click 'Add'
* Remote name: hol353   <- Can be anything, but we recommend using the username of your GitHub account.
* URL / Path: https://github.com/hol353/ApsimX <- URL of your ApsimX fork
* Host Type: GitHub
* Username: hol353       <- GitHub user name 

We suggest you name the remote repository the same as your GitHub user name, hence the need to enter it twice. The reason for linking to two repositories will become evident later. You ALWAYS **pull** from the ApsimX repository and **push** to your forked repository.

At this point, you have all source code. If you wish to compile the code yourself, see [here](/contribute/compile/). If you don't wish to compile the code, you can run any of the examples/prototypes/test sets with the released version of apsim, but it will need to be up-to-date.

After you have made some changes to the code or test sets, you will need to [commit your changes](/contribute/sourcetree/commit/).
