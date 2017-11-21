---
title: "Get Source"
draft: false
---

# Getting the source code

The APSIM source code is located on GitHub so you will need to have a GitHub login if you want to contribute new features or modifications to the APSIM repository.

There are two repositories: 

[https://github.com/APSIMInitiative/ApsimX](https://github.com/APSIMInitiative/ApsimX) - This contains the main APSIM source code of the infrastructure and all models.

[https://github.com/APSIMInitiative/APSIM.Shared](https://github.com/APSIMInitiative/APSIM.Shared) - This contains many utilities and classes that are shared between many projects.

Both of these will need to be 'forked' to your GitHub account. A fork creates a clone of the main repositories. Look for the fork link in the top right hand corner of the APSIM repository on GitHub. Clicking this will create a copy of the APSIM repository in your GitHub account.

To bring the source code from your GitHub account to your computer, you will need to clone the repository. We recommend you use a Git client. We recommend [SourceTree](http://www.sourcetreeapp.com) for this. Once you have SourceTree installed on your computer, goto *File | Clone* menu item.


Click the 'Clone / New' button in SourceTree and specify the URL for your GitHub fork. e.g.

![SourceTreeClone](/images/Development.SourceTreeClone.png)

You then need to do the same for the APSIM.Shared repository. The ApsimX and APSIM.Shared folders should be sibling folders on your computer e.g. you if used c:\Work as the root folder for the two repositories, you should end up with a c:\Work\ApsimX and a c:\Work\Apsim.Shared folder.