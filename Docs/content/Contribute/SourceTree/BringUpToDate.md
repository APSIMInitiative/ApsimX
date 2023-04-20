---
title: "4. Bring folder up to date"
draft: true
---

This document assumes that your APSIM Next Generation folder is C:\Work\ApsimX

**NOTE:** Before you bring your folder up to date, you need to [commit or discard all files that you have added or modified](/development/commit). If you don't do this you may get errors during the pull process outlined below.

## Pull

To bring the current branch up to date you get the latest commits from the *master* branch in the *MasterRepo* repository. Click the pull button:

![SourceTreeRemotes](/images/Development.SourceTreePull.png)

Ensure the remote is *MasterRepo* and the branch is *master*. Leave all other checkboxes alone. This will bring down the latest commits from the MasterRepo/master branch into your 'current' branch (the one in bold in SourceTree).

**RECOMMENDATION:** You should always bring your branch up to date at the beginning of a major piece of work. In addition, you should also do a pull regularly, at least weekly.