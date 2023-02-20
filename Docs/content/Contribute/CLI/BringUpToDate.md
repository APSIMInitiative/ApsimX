---
title: "4. Bring folder up to date"
draft: true
---

**NOTE:** Before you bring your folder up to date, you need to [commit or discard all files that you have added or modified](/contribute/cli/commit). If you don't do this you may get errors during the pull process outlined below.

## Pull

Pulling from a remote branch will update a local branch to reflect the latest changes made in the remote branch.

**RECOMMENDATION:** You should always bring your branch up to date at the beginning of a major piece of work. In addition, you should also do a pull regularly, at least weekly.

You will usually want to pull from the master branch in the main APSIM repository.

To list your remote repositories and their associated URLs, use ````git remote -v````

Then simply run ````git pull <remote> <branch>````, where \<remote\> and \<branch\> are the names of the repository and branch you wish to pull from.
