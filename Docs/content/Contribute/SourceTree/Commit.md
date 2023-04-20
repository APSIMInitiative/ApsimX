---
title: "2. Commit files"
draft: true
---

## Initial setup

**The first time you use SourceTree** we recommend you turn staging off.

* Click 'Commit'
* Choose 'No staging' in the drop down beside the 'Modified files...' drop down

![SourceTreeRemotes](/images/Development.SourceTreeNoStaging.png)

## Commit

If you have files that you have added or modified, you can commit them to git. This process of adding commits keeps track of your progress as you work on files in APSIM.

Commits also create a transparent history of your work that others can follow to understand what you've done and why. Each commit has an associated commit message, which is a description explaining why a particular change was made. Furthermore, each commit is considered a separate unit of change. This lets you roll back changes if a bug is found, or if you decide to head in a different direction.

Commit messages are important, especially since Git tracks your changes and then displays them as commits once they're pushed to the server. By writing clear commit messages, you can make it easier for other people to follow along and provide feedback.

Commits are local to your computer only until you do a push to a remote repository. 

![SourceTreeRemotes](/images/Development.SourceTreeCommit.png)

In the top left corner, SourceTree shows (by default) the files that you have modified but haven't commited yet. If you have created new files that have never been commited they won't be shown yet. To see these files, change the drop down box from 'Modified files" to "Untracked". Note that the .db files produced by APSIM simulations are ignored by git and should not be committed.

* You can then right click on the untracked files and select "Add" to tell git to start tracking them.
* If you don't want to keep them, right click and select "Remove". **This will delete them from your folder**
* Change the drop down back to "Modified files"

Clicking on a file will show you what you have changed. If you don't want to keep the changes you have made, right click on the file and select 'Discard'. 

Tick the files you want to commit.

Finally, at the bottom type in a commit message, make sure 'Push changes immediately...' is **unticked** and then click Commit. At this point, your new commit is only on your computer and noone else can see it. You can commit files as many times as you wish.
