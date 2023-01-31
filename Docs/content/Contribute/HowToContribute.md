---
title: "How to contribute"
date: 2023-01-30T15:07:31+10:00
draft: false
---

# 1. Getting APSIM source code

## Create a GitHub Account

The APSIM source code is located on GitHub so you will need to have a GitHub login if you want to contribute new features or modifications to the APSIM repository.

To create an account head to <a href="https://www.github.com/" target="_blank">Github</a>.


# 2. Making and getting changes

## Download and install Git

Git is used to get the APSIM source code and to keep it up to date.

You can download git <a href="https://git-scm.com/downloads">here</a>.

## Using Git to make changes

Instructions on how to use Git to make and submit changes can be found <a href="https://git-scm.com/doc" target="_blank">here</a>

## Cloning the repository

The first thing to do is to clone the <a href="https://github.com/APSIMInitiative/ApsimX" target="_blank">APSIMInitiative/ApsimX</a> repository. This can be done by using a command line terminal to navigate to the directory you want to download the source code to and run the command: 

	git clone https://github.com/APSIMInitiative/ApsimX.git
	
Alternatively, you can use Visual Studio to clone the repository from the start screen

![Clone repo in visual studio](/images/clone_repo_visual_studio)
	
It is best practice to fork(copy) the APSIM repository and push changes to this before submitting changes to the master version of APSIM. 

See <a href="#to-create-a-fork">"To Create a Fork"</a> section below. 

## Working on Apsim

To see how to begin working on APSIM for your unique operating system see <a href="/contribute/compile/" target="_blank">Compile Section</a>


# 3. Contributing your changes

You can’t push directly to the main ApsimX repository. Instead, you need to push to your remote fork (copy) of ApsimX. 

## To create a fork:

- Open a web browser, go to the <a href="https://github.com/APSIMInitiative/ApsimX" target="_blank">APSIM github page</a> and click on the fork link in the top right hand corner of the repository on GitHub. 
- Clicking this will create a copy of the APSIM repository into your GitHub account.
	![fork repo](/images/fork_repo.png)
- Once you’ve done this you need to add your GitHub ‘remote’ to your git client. We recommend Fork. You can download it <a href="https://git-fork.com/" target="_blank"> here</a>
- In the screenshot below, right click on ‘Remotes’ in the tree, click ‘Add Remote’ and fill in a name for your remote (usually your name or your github name) and the URL for the ApsimX repo. Mine looks like:
	
	![add remote](/images/add_remote.png)
	
- 'ric394' is my github user name. You can then push to your remote (rather than origin). Click push and change ‘remote’ drop down to your newly created one. Mine looks like:
	
	![push to remote](/images/push_to_remote.png)
	
- After pushing you need to create a pull request. Right click on your ‘master’ branch and choose create pull request:
	
	![pull request](/images/pull_request.png)
	
- This will open a browser window where you need to enter a comment in the top comment box. 

	![Create pull request](/images/create_pull_request.png)
	
	- You can reference issue numbers here. 
	
	- It is also good to add some extra comments in the top box that explains what is in the pull request e.g. New cotton validation data from site xyz. 
	
	- Once done, click the ‘Create Pull Request’ button at the bottom of the browser window. 
	
	- Once the PR has been created, everyone can see it automatically and a peer-review will be performed. 
	
	- It will also automatically trigger a build and test of our test suite. If it is approved, it will be merged it into the main master branch and it will be made available to all users.

This looks complicated but once you’ve created a few pull requests you’ll get the hang of it.


