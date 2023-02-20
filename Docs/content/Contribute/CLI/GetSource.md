---
title: "1. Get source code"
draft: true
---

## Create GitHub account

The APSIM source code is located on GitHub so you will need to have a GitHub login if you want to contribute new features or modifications to the APSIM repository.

This document assumes that your APSIM Next Generation folder is ~/ApsimX/.

## (Optional) Fork the two repositories

There are two repositories: 

1. [https://github.com/APSIMInitiative/ApsimX](https://github.com/APSIMInitiative/ApsimX) - This contains the main APSIM source code of the infrastructure and all models.

2. [https://github.com/APSIMInitiative/APSIM.Shared](https://github.com/APSIMInitiative/APSIM.Shared) - This contains many utilities and classes that are shared between many projects.

Both of these will need to be 'forked' to your GitHub account if you plan to change files in both repositories.


* Click on the fork link in the top right hand corner of the [APSIM repository](https://github.com/APSIMInitiative/ApsimX]) on GitHub. Clicking this will create a copy of the APSIM repository in your GitHub account.

## Install git

First, you will need to install the git client (if you don't already have it installed): 

```sudo apt install git```
	
## Clone the two Repositories

To bring the source code from GitHub to your computer, you will need to clone the two repositories. The ApsimX and APSIM.Shared directories should be siblings. For example if you clone ApsimX to ~/ApsimX, you should clone APSIM.Shared to ~/APSIM.Shared.

```
git clone https://github.com/APSIMInitiative/ApsimX
git clone https://github.com/APSIMInitiative/APSIM.Shared
```

## Add a remote repository

If you forked either of the repositories, you will need to add your remote repository:

```
cd ApsimX
git remote add $remote_name https://github.com/$username/ApsimX
```

Replace $username with your github username and $remote_name with a name of your choosing. This will be the name you use to refer to your remote repository. You will need to perform this step for both repositories if you forked both.