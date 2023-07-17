---
title: "3. Merge changes"
draft: true
---

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
