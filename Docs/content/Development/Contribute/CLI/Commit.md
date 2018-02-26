---
title: "2. Commit files"
draft: false
---

## Commit

If you have files that you have added or modified, you can commit them to GIT. This process of adding commits keeps track of your progress as you work on files in APSIM.

Commits also create a transparent history of your work that others can follow to understand what you've done and why. Each commit has an associated commit message, which is a description explaining why a particular change was made. Furthermore, each commit is considered a separate unit of change. This lets you roll back changes if a bug is found, or if you decide to head in a different direction.

Commit messages are important, especially since Git tracks your changes and then displays them as commits once they're pushed to the server. By writing clear commit messages, you can make it easier for other people to follow along and provide feedback.

Commits are local to your computer only until you do a push to a remote repository. 

To view the changes from the previous commit, navigate to your ApsimX directory and enter:

````git diff````

To commit your changes:

````git commit -m "YourMessage"```` (where YourMessage is your commit message)


For additional options/help, use ````git help commit```` or ````man git````
