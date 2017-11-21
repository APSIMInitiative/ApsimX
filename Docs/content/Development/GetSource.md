---
title: "GIT 1: Get source code from GitHub into new folder"
draft: false
---

## Create GitHub account

The APSIM source code is located on GitHub so you will need to have a GitHub login if you want to contribute new features or modifications to the APSIM repository.

## Fork the two repositories

There are two repositories: 

1. [https://github.com/APSIMInitiative/ApsimX](https://github.com/APSIMInitiative/ApsimX) - This contains the main APSIM source code of the infrastructure and all models.

2. [https://github.com/APSIMInitiative/APSIM.Shared](https://github.com/APSIMInitiative/APSIM.Shared) - This contains many utilities and classes that are shared between many projects.

Both of these will need to be 'forked' to your GitHub account. A fork creates a clone of the main repositories. Look for the fork link in the top right hand corner of the APSIM repository on GitHub. Clicking this will create a copy of the APSIM repository in your GitHub account.

## Clone both repositories to your computer

To bring the source code from your GitHub account to your computer, you will need to clone the repository. We recommend you use a Git client. We recommend [SourceTree](http://www.sourcetreeapp.com) for this. Once you have SourceTree installed on your computer, goto *File | Clone* menu item.


Click the 'Clone / New' button in SourceTree and specify:

* Source Path / URL: https://github.com/APSIMInitiative/ApsimX
* Destination Path: C:\Work\ApsimX   <- this is the folder on your computer 
* Name: MasterRepo   <- This is the name that this respository will be known by

Once you click 'Clone', all files will be downloaded to your computer into the destination path that you specified above. SourceTree will create a tab for your new clone.

## Add a second remote repository

If you look at your repository settings (Repository | Repository Settings menu) you will see a single remote repository called MasterRepo that points to the main ApsimX repository. You now need to create a link to another remote repository, the one you 'forked' earlier. Click 'Add' and enter:

* Remote name: hol353   <- the username of your GitHub account.
* URL / Path: https://github.com/hol353/ApsimX
* Host Type: GitHub
* Username: hol353       <- GitHub user name 

We suggest you name the remote repository the same as your GitHub user name, hence the need to enter it twice. The reason for linking to two repositories will become evident later. You ALWAYS **pull** from the ApsimX repository and **push** to your forked repository.

## APSIM.Shared
 
You then need to do the same for the APSIM.Shared repository. The ApsimX and APSIM.Shared folders should be sibling folders on your computer e.g. you if used C:\Work as the root folder for the two repositories, you should end up with a C:\Work\ApsimX and a C:\Work\Apsim.Shared folder.

### Clone settings

Click the 'Clone / New' button in SourceTree and specify:

* Source Path / URL: https://github.com/APSIMInitiative/APSIM.Shared
* Destination Path: C:\Work\APSIM.Shared 
* Name: MasterRepo

Once all files have downloaded, create a link to a second repository:

* Remote name: hol353   <- the username of your GitHub account.
* URL / Path: https://github.com/hol353/APSIM.Shared
* Host Type: GitHub
* Username: hol353       <- GitHub user name 