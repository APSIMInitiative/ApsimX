---
title: "CLI"
weight: 40
---

This is a guide to using version control from the command line. If you are stuck at any time, a useful git cheat sheet may be found [here](https://www.atlassian.com/git/tutorials/atlassian-git-cheatsheet).



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


## Commit

If you have files that you have added or modified, you can commit them to git. This process of adding commits keeps track of your progress as you work on files in APSIM.

Commits also create a transparent history of your work that others can follow to understand what you've done and why. Each commit has an associated commit message, which is a description explaining why a particular change was made. Furthermore, each commit is considered a separate unit of change. This lets you roll back changes if a bug is found, or if you decide to head in a different direction.

Commit messages are important, especially since git tracks your changes and then displays them as commits once they're pushed to the server. By writing clear commit messages, you can make it easier for other people to follow along and provide feedback.

Commits are local to your computer only until you do a push to a remote repository.

To list new/modified files use `git status`

To view the changes from the previous commit, use `git diff`

To undo changes which you don't want to keep, use `git checkout ModifiedFile.txt`

Before you commit your changes you must first add any new or modified files to the index.

```
git add ModifiedFile.txt
```

To remove a file from the index, use the reset command:

```
git reset ModifiedFile.txt
```

To perform the commit:

```
git commit -m "Commit message"
```

For additional options/help, use `git help commit` or `man git`



**NOTE:** Before you bring your folder up to date, you need to [commit or discard all files that you have added or modified](/contribute/cli/commit). If you don't do this you may get errors during the pull process outlined below.

## Pull

Pulling from a remote branch will update a local branch to reflect the latest changes made in the remote branch.

**RECOMMENDATION:** You should always bring your branch up to date at the beginning of a major piece of work. In addition, you should also do a pull regularly, at least weekly.

You will usually want to pull from the master branch in the main APSIM repository.

To list your remote repositories and their associated URLs, use ````git remote -v````

Then simply run ````git pull <remote> <branch>````, where \<remote\> and \<branch\> are the names of the repository and branch you wish to pull from.


## Push changes to a remote

Once you're ready to share your commits with the wider APSIM community and have them merged into the master repository, you will need to push your commits to your forked remote repository (e.g. hol353). Doing a push won't impact on other developers and won't cause Jenkins to run the test suite. Pushing will allow other developers to pull from your branch on your repository so it is a good way to share what you are doing with others.

To list your remote repositories, use ```git remote```. To view remote URLs as well, use ```git remote -v```.

**You should never push to the main APSIM repository** (located at https://github.com/APSIMInitiative/ApsimX/).

To push your current branch to a remote repository, use ```git push <remote>```, where \<remote\> is the name of your remote repository. If you have not pushed your current branch before, you will need to create a remote branch: ```git push -u <remote> <branch_name>```.

For more details, see ````git help push````.

## Open a Pull Request

Pull Requests initiate discussion about your commits. They say to other developers that you are wanting a peer review of your changes.

You can open a Pull Request at any point during the development process: when you have little or no code but want to share some screenshots or general ideas, when you're stuck and need help or advice, or when you're ready for someone to review your work. By using GitHub's @mention system in your Pull Request message, you can ask for feedback from specific people or teams, whether they're down the hall or ten time zones away.

To open a pull request, open a web browser and navigate to your remote repository (or the APSIM repository under pull requests tab) on GitHub, then click New Pull Request.

### 1. Peer review

Once a Pull Request has been opened, a developer will review your changes and may have questions or comments. Perhaps the coding style doesn't match project guidelines, the change is missing unit tests, or maybe everything looks great. Pull Requests are designed to encourage and capture this type of conversation.

You can also continue to push to your branch in light of discussion and feedback about your commits. If someone comments that you forgot to do something or if there is a bug in the code, you can fix it in your branch and push up the change. GitHub will show your new commits and any additional feedback you may receive in the unified Pull Request view.

### 2. Jenkins build and run system

Jenkins will automatically run all pull requests and flag pass/fail with GitHub. If you have finished a piece of work then you need to state somewhere in the first comment box of the pull request:

Resolves #45

or

Working on #45

This will alert the administrators of the APSIM repository that the pull request fixes issue number 45 (or you are working on it). All merges to master must have an issue describing the piece of work.

### 3. The APSIM Performance Testing site

The APSIM Performance Testing suite will also test your pull request, calculating statistics on all predicted / observed data found and check them against the 'accepted' statistics. This will also be reported back to your pull request.

## Merging with the MasterRepo

If the pull request has been reviewed by a developer, the Jenkins build system passes and the APSIM Performance Testing system also passes, the administrators will then merge the pull request with the master branch of the main repository and close the issue (if you specified 'resolves'). Once the issue is closed it should not be reopened.

After a Pull request that resolves an issue is authorised to be merged, the the automated upgrade building process will commence to create an upgrade available in the upgrade manager of the user interface. The upgrade make take a while to generate and has the following naming: [Date of merge yyyy.mm.dd].[resolved issue number] "Issue description" (e.g. 2021.08.12.6699 Predicted-observed graphs not displaying).
