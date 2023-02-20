---
title: "2. Commit files"
draft: true
---

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
