---
title: "Branching And Git"
draft: false
---

# Branching and GIT

We use a feature branch work flow. The following description is borrowed from [Understanding the GitHub flow](https://guides.github.com/introduction/flow).

## Branching
When you're working on APSIM, you're going to have a bunch of different features or ideas in progress at any given time – some of which are ready to go, and others which are not. Branching exists to help you manage this workflow.

When you create a branch in your project, you're creating an environment where you can try out new ideas. Changes you make on a branch don't affect the master branch, so you're free to experiment and commit changes, safe in the knowledge that your branch won't be merged until it's ready to be reviewed by someone you're collaborating with.

Branching is a core concept in Git, and the entire GitHub Flow is based upon it. There's only one rule: anything in the master branch is always deployable. Because of this, it's extremely important that your new branch is created off of master when working on a feature or a fix. Your branch name should be descriptive (e.g., refactor-plant-leaf,soilwater-defect, new-maize-model), so that others can see what is being worked on. Branch button in SourceTree can be used to create a branch. Your current branch is in bold in SourceTree.

![SourceTreeBranches](/images/Development.SourceTreeBranches.png)

To switch between branches, commit your changes and then double click the branch you want to switch to. Branches are independent of each other. Your changes on one branch will be unavailable in the other branch. At this stage the branches only exist on your GIT clone on your hard disk.
 
## Add commits

Once your branch has been created, it's time to start making changes. Whenever you add, edit, or delete a file, you're making a commit, and adding them to your branch. This process of adding commits keeps track of your progress as you work on a feature branch.

Commits also create a transparent history of your work that others can follow to understand what you've done and why. Each commit has an associated commit message, which is a description explaining why a particular change was made. Furthermore, each commit is considered a separate unit of change. This lets you roll back changes if a bug is found, or if you decide to head in a different direction.

Commit messages are important, especially since Git tracks your changes and then displays them as commits once they're pushed to the server. By writing clear commit messages, you can make it easier for other people to follow along and provide feedback.

Commits are local only until you do a push.

## Bringing branches up to date

If you have performed commits in a branch on your hard disk and want to bring the branch up to date you can get the latest changes from the master branch in the *orgin* repository. **Before you can do this you need to commit any changes.** Once, you've done that you can do a pull. Click the pull button:

![SourceTreeRemotes](/images/Development.SourceTreePull.png)

Ensure the remote is *orgin* and the branch is *master*. Leave all other checkboxes alone. This will bring down the latest changes from the origin/master branch into your 'current' branch (the one in bold in SourceTree).

## Push changes to a remote

Once you're ready make changes available to others, you will need to push your commits to a 'remote' repository. This is usually your ApsimX fork in your repository. Doing this won't impact on other developers and won't cause Jenkins to run the test suite. You can do this as many times as you wish.

![SourceTreeRemotes](/images/Development.SourceTreeRemotes.png)

In this image there are two remote repositories, hol353 (a developers remote) and orgin (the main APSIM repository). You should never push to the *origin* remote. Instead, you should push to your remote - *hol353* in this example. To push, click the 'Push' button in SourceTree.

![SourceTreeRemotes](/images/Development.SourceTreePush.png)

Always make sure the remote (highlighted in the above image) is your remote and not *origin*. You also need to tick the branch you want to push to your remote repository.

## Open a Pull Request

Pull Requests initiate discussion about your commits. Because they're tightly integrated with the underlying Git repository, anyone can see exactly what changes would be merged if they accept your request.

You can open a Pull Request at any point during the development process: when you have little or no code but want to share some screenshots or general ideas, when you're stuck and need help or advice, or when you're ready for someone to review your work. By using GitHub's @mention system in your Pull Request message, you can ask for feedback from specific people or teams, whether they're down the hall or ten time zones away.

Pull Requests are useful for contributing to open source projects and for managing changes to shared repositories. Pull Requests provide a way to notify project maintainers about the changes you'd like them to consider. They can also help start code review and conversation about proposed changes before they're merged into the master branch.

## Discuss and review your code

Once a Pull Request has been opened, the person or team reviewing your changes may have questions or comments. Perhaps the coding style doesn't match project guidelines, the change is missing unit tests, or maybe everything looks great and props are in order. Pull Requests are designed to encourage and capture this type of conversation.

You can also continue to push to your branch in light of discussion and feedback about your commits. If someone comments that you forgot to do something or if there is a bug in the code, you can fix it in your branch and push up the change. GitHub will show your new commits and any additional feedback you may receive in the unified Pull Request view.

## Jenkins automated testing

Jenkins will automatically run all pull requests and flag pass/fail with GitHub. If you have finished a piece of work then you need to state somewhere in the body of the pull request:

Resolves #45

This will alert the administrators of the APSIM repository that the pull request fixes issue number 45. All merges to master must have an issue describing the piece of work. The administrators will then merge the pull request with the master branch of the main repository and close the issue. Once the issue is closed it should not be reopened.