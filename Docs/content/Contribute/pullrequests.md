---
title: Pull request guidelines
draft: false
weight: 20
---

### Pull Request (PR) Priorities

* The software team prioritise defect fixes above new features. This is in line with the software teams' zero bug policy.
* New features are then prioritized.

### Pull request best practices

* Short-lived (1-2 weeks max) feature branches are ideal. Branches that are used to develop a new plant are an exception here.
* Aim to make smaller and more frequent PRs over larger less frequent PRs. Smaller PRs tend to be easier to review. Larger pieces of work can usually be broken into smaller feature branches which are easier to manage.
* Regularly pull in changes from the APSIMInitiative/master branch.
* Ensure a pull request is close to the tip revision i.e. ensure the most recent version of repo is pulled into the PR branch.
* A pull request must only do one thing.
* A pull request should briefly and concisely describe what the issue was, what changes have been made and the rationale.
* Changes to observed data, or the addition of new data, should be on a PR with no other changed files. This allows the software team to clearly see the effect of the changed/new data without it being confounded by other changes.
* **Science** changes require observed data to demonstrate that the change does what it intends. Additionally unit tests are to be included.
* **Graphical user interface (GUI)** changes and fixes that include a short video showing the changes working will improve review time.
* **Bug fixes** should include a unit test to reduce the likelihood of recurrence and to also verify the fix.

## Things to Avoid

* Keeping branches in development for long periods of time. Branches that have been left and need to be brought up to date with the newest changes can be difficult to update.
* Submitting pull requests that resolve multiple unrelated issues in the one pull request. Doing so increases the difficulty of review.
* Submitting pull requests that contain multiple new features. This also increases the review time.
* Leaving PRs inactive for long lengths of time (60+ days). PRs not marked with the following labels will be closed automatically to help with PR review efforts: `Ready for Reference Panel Comment, Do Not Merge, High Level Review, Low Level Review, Ready for Software Review`
