---
title: "1. Get source code from GitHub"
draft: false
---

## Create GitHub account

The APSIM source code is located on GitHub so you will need to have a GitHub login if you want to contribute new features or modifications to the APSIM repository.

This document assumes that your APSIM Next Generation folder is C:\Work\ApsimX.

## Fork the two repositories

There are two repositories: 

1. [https://github.com/APSIMInitiative/ApsimX](https://github.com/APSIMInitiative/ApsimX) - This contains the main APSIM source code of the infrastructure and all models.

2. [https://github.com/APSIMInitiative/APSIM.Shared](https://github.com/APSIMInitiative/APSIM.Shared) - This contains many utilities and classes that are shared between many projects.

Both of these will need to be 'forked' to your GitHub account if you plan to change files in both repositories. A fork creates a clone of the main repositories. 

* Click on the fork link in the top right hand corner of the [APSIM repository](https://github.com/APSIMInitiative/ApsimX]) on GitHub. Clicking this will create a copy of the APSIM repository in your GitHub account.

## Clone ApsimX to your computer

To bring the source code from your GitHub account to your computer, you will need to clone the repository. We recommend you use a Git client. We recommend [SourceTree](http://www.sourcetreeapp.com) for this. Once you have SourceTree installed on your computer, goto *File | Clone* menu item.


Click the 'Clone / New' button in SourceTree and specify:

* Source Path / URL: https://github.com/APSIMInitiative/ApsimX
* Destination Path: C:\Work\ApsimX   <- this is the folder on your computer 
* Name: MasterRepo   <- This is the name that this respository will be known by

Once you click 'Clone', all files will be downloaded to your computer into the destination path that you specified above. SourceTree will create a tab for your new clone.

You then need to change the default name that SourceTree shows to something more meaningful

* Goto Repository | Repository settings menu
* Select the origin repository and click Edit
* Untick 'Default remote' in the top right corner and change the remote name to MasterRepo
* Click OK.
* You now need to create a link to your ApsimX fork that you created earlier. Click 'Add'
* Change Remote name: hol353   <- the username of your GitHub account.
* Change URL / Path: https://github.com/hol353/ApsimX
* Change Host Type: GitHub
* Change Username: hol353       <- GitHub user name 

We suggest you name the remote repository the same as your GitHub user name, hence the need to enter it twice. The reason for linking to two repositories will become evident later. You ALWAYS **pull** from the ApsimX repository and **push** to your forked repository.

## Clone APSIM.Shared to your computer
 
You then need to do the same for the APSIM.Shared repository. The ApsimX and APSIM.Shared folders should be sibling folders on your computer e.g. you if used C:\Work as the root folder for the two repositories, you should end up with a C:\Work\ApsimX and a C:\Work\Apsim.Shared folder.


* Click the 'Clone / New' button in SourceTree
* Specify source Path / URL: https://github.com/APSIMInitiative/APSIM.Shared
* Specify Destination Path: C:\Work\APSIM.Shared 
* Specify Name: MasterRepo


You then need to change the default name that SourceTree shows to something more meaningful

* Goto Repository | Repository settings menu
* Select the origin repository and click Edit
* Untick 'Default remote' in the top right corner and change the remote name to MasterRepo
* Click OK.

At this point, you have all source code. If you plant on modifying APSIM.Shared, you need to create a link to your APSIM.Shared fork that you created earlier. 

* Goto Repository | Repository settings menu
* Click 'Add'
* Remote name: hol353   <- the username of your GitHub account.
* URL / Path: https://github.com/hol353/APSIM.Shared
* Host Type: GitHub
* Username: hol353       <- GitHub user name 