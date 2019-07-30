---
title: "1. Get source code"
draft: false
---

## Create GitHub account

The APSIM source code is located on GitHub so you will need to have a GitHub login if you want to contribute new features or modifications to the APSIM repository.

This document assumes that your APSIM Next Generation folder is ~/ApsimX/.

## Fork the two repositories

There are two repositories: 

1. [https://github.com/APSIMInitiative/ApsimX](https://github.com/APSIMInitiative/ApsimX) - This contains the main APSIM source code of the infrastructure and all models.

2. [https://github.com/APSIMInitiative/APSIM.Shared](https://github.com/APSIMInitiative/APSIM.Shared) - This contains many utilities and classes that are shared between many projects.

Both of these will need to be 'forked' to your GitHub account if you plan to change files in both repositories. A fork creates a clone of the main repositories. 


* Click on the fork link in the top right hand corner of the [APSIM repository](https://github.com/APSIMInitiative/ApsimX]) on GitHub. Clicking this will create a copy of the APSIM repository in your GitHub account.

## Install git

First, you will need to install the git client (if you don't already have it installed): 

````sudo apt-get install git````
	
## Clone ApsimX to your computer

First, navigate to the directory you wish to clone ApsimX to:

````cd ~/````
	
To bring the source code from your GitHub account to your computer, you will need to clone the repository. 

````git clone https://github.com/hol430/ApsimX ApsimX```` (replace this URL with the URL of your repository)

## Clone APSIM.Shared to your computer
 
You then need to do the same for the APSIM.Shared repository. The ApsimX and APSIM.Shared folders should be sibling folders on your computer e.g. you if used ~/ as the root folder for the two repositories, you should end up with a ~/ApsimX and a ~/APSIM.Shared folder.

````git clone https://github.com/hol430/APSIM.Shared APSIM.Shared```` (replace this URL with the URL of your repository)

At this point, you have all source code. If you plant on modifying APSIM.Shared, you need to create a link to your APSIM.Shared fork that you created earlier. 

If you require additional help, use ````git help clone```` or ````man git````

## Add a remote repository

You will also need to add the master APSIM repository as a remote repository, in order to pull from it. For example:

````cd ApsimX````

````git remote add MasterRepo https://github.com/APSIMInitiative/ApsimX````

This will add the main APSIM repository as a remote repository called MasterRepo.