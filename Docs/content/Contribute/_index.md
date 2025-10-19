---
title: "Contribute"
draft: false
weight: 20
---

This outlines the APSIM review process for all contributions to GitHub.

### Step 1 Developer raises a GitHub Issue

* Describe the issue and functionality required with an emphasis on “Why” the change is required i.e. the use-case.
* Raise the issue early (well before development) to allow input from others.
* For a user interface defect, create a small video that demonstrates the problem.
* Ensure code conforms to the [Issue guidelines](/contribute/issues)

### Step 2 Issue is reviewed by software team

The software team will:

* Review the issue for clarity
* Check to see if it is a duplicate of an existing issue.
* Optionally tag ```@APSIMInitiative/reference-panel``` if issue might be of interest to the Reference Panel.

### Step 3 Developer raises a GitHub Pull Request (PR)

* Ensure the PR conforms to the [PR guidelines](/contribute/pullrequests)
* The developer must link to an issue. Use ```working on #issue``` or ```resolves #issue```)
* Ensure the PR matches the Issue
* The build/test system will run automatically and set a green/red (pass/fail) status.
* The developer may need to push changes multiple times to resolve errors.
* When development is complete, the developer adds a ```Ready for Review``` label.

### Step 4 High Level Review by software team

The software team will:

* Add ```High Level Review``` label
* Ensure the PR matches the Issue
* Suggest design improvements (e.g. missing converter)
* Ask for additional tests if necessary
* Ensure PR conforms to the [PR guidelines](/contribute/pullrequests)

This review may take several iterations. Once the team is satisfied, a low level review can take place.

### Step 5 Low Level Review

The software team will:

* Perform a full code review
* Initiative a Co-pilot review
* Suggest [style guideline](#style-guidelines) fixes
* Review the tests
* Review any changes in statistics
* Check for inadvertent changes in statistics
* Check for inadvertent changes in code

__If this is a software change only__
* The software team will merge once resolved.
  
__Else if a minor science change__
e.g. addition to test datasets or simple changes or corrections to model or addition of new outputs
* Tag ```@APSIMInitiative/reference-panel``` FYI
* The software team will merge the PR within 3 business days.

__Else if this is a significant change to science code or data sets__
e.g. a new model or significant changes to a published model.
* The software team will tag ```@APSIMInitiative/reference-panel``` for additional review
* Move to Step 6.

### Step 6 Reference Panel Review

The software team will:

* Add ```Reference Panel Review``` label to PR
* Contact the Reference Panel coordinator to put a PR review on the agenda for the next meeting
* The software team will assist the developer to ensure all code, tests, documentation, stats should be ready for review by the Reference Panel.
* Most of the meaningful interactions could happen on GitHub prior to the meeting.
* The Reference Panel will review the changes and make suggested improvements.
* Once completed, the process will return to Step 5.

Further details can be found in [Science contributions](/contribute/science-contributions)
