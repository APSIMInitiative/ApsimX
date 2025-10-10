---
layout: page
title: Pull Requests and Code Submission
permalink: /pull-requests/
---

# Pull requests and code submissions

## Pull Request Priorities

* Bug fix PRs that stop APSIM users from being able to use the software are top priority. This is in line with the software teams' zero bug policy.
* Other bugs that only affect a portion of the software are the next highest priority.
* New features are then prioritized.

## Getting a Pull request reviewed

* To have the software team review a pull request attach the `Ready for Software Review` label.
* It is best practice to rerun a pull request if it is behind the main branch. This ensures it runs with the newest changes.

## Pull request requirements and best practices

* Each pull request must only do one thing.
* The requirements for a pull request differ based on the changes submitted.
  * **Science** changes require observed data to demonstrate that the change does what it intends. Additionally unit tests are to be included.
  * **Graphical user interface (GUI)** changes and fixes that include a short video showing the changes working will improve review time.
  * **Bug fixes** should include a unit test to reduce the likelihood of recurrence and to also verify the fix.
* All pull requests should briefly and concisely describe what the issue was, what changes have been made and the rationale.

## Adding new simulations and apsim files

### Making changes to existing datasets

* To enable a fast and thorough review of changes when new files and data are added, it is best to first create a pull request that only adds the data followed by another pull request that adds the apsim file. This is to allow a clearer review that allows reviewers to determine which files changed statistics and helps with debugging when required.

## Pull request process

This section describes in detail how a pull request is handled.

1. A pull request is submitted with a `Ready for Software Review` label. If you are unable to apply labels, request to be added to the developers github team by submitting a comment in the pull request.
2. Validation and user tests are run.
3. Once status checks all run successfully, a peer review is performed by the software team. This is conducted in two distinct steps.
4. The `High Level Review` will be applied while the PR is reviewed from a high level. Details on what a high level review entails are available under the [high level review details section](#step-4-high-level-review)
5. Once any issues (if any) are resolved, the `Low Level Review` label will be applied and a low level review conducted. Details on what a low level review entails are available under the [low level review details section](#step-5-low-level-review)  
6. If issues are found or need further discussion the `Ready for Software Review` will be removed and the `More information needed` label will be applied to the PR.
7. Once the issues have been resolved and discussion is complete the pull request author should reapply the `Ready for Software Review` label.
8. Once reviewed (and if a Reference Panel review is needed and conducted) the pull request will be merged.

## Changing apsimx files

* When fixing an issue with `.apsimx` files avoid opening files and making changes directly. Instead create a converter so that all affected `.apsimx` files will be changed automatically. Additionally, resource files (models loaded from json files) will need to be changed and this is best done by using the `update resources` button in the main menu.
