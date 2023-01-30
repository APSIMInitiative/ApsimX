---
title: "How to contribute"
date: 2023-01-30T15:07:31+10:00
draft: false
---
You can’t push directly to the main ApsimX repository. Instead, you need to push to your remote fork (copy) of ApsimX. 

To create a fork:

- Open a web browser, go to https://github.com/APSIMInitiative/ApsimX and click on the fork link in the top right hand corner of the repository on GitHub. 
- Clicking this will create a copy of the APSIM repository into your GitHub account.
	![fork repo](/images/fork_repo.png)
- Once you’ve done this you need to add your GitHub ‘remote’ to your ‘Fork’ app (or alternative) on Windows. [Get Fork here](https://git-fork.com/)
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


