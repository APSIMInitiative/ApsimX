# General Good Practices

## Things to Do :white_check_mark:

* As the project follows the agile methodology, short-lived (1-2 weeks max) feature branches are ideal. Branches that are used to develop a new plant are an exception here.
* Aim to make smaller and more frequent PRs over larger less frequent PRs. Smaller PRs tend to be easier to review. Larger pieces of work can usually be broken into smaller feature branches which are easier to manage.
* Regularly pull in changes from the APSIMInitiative/master branch.

## Things to Avoid :x:

* Keeping branches in development for long periods of time. Branches that have been left and need to be brought up to date with the newest changes can be difficult to update.
* Submitting pull requests that resolve multiple unrelated issues in the one pull request. Doing so increases the difficulty of review.
* Submitting pull requests that contain multiple new features. This also increases the review time.

## Reporting and Creating Issues

### Best practices

* The issue title is important. It must be a sentence accurately describing the issue. It is also important as it si the version description for an APSIM release version. Having a good description aids users in deciding what release version to download.
* When submitting an issue include the apsim file with any required met and input files required to make it run in a zip archive.
* Describe the steps that lead to the issue occurring so that it can be reproduced. This includes details like where you clicked, what settings were enabled and what operating system it occurred on.
* For graphical user interface (GUI) issues please submit a minimally reproducible example apsim file, describe the user interface model this issue affects and the steps to reproduce the error.
* Describe issues with an emphasis on the "Why" more than "What" or "How". The issue description should be clear enough that another person should be able to fix the problem or create the feature.
* Submit issues before you begin creating a solution. This has the advantage of avoiding unnecessary effort if the issue is found to be resolved or in the process of being resolved and allows others to suggest possible approaches and considerations that can improve the subsequent PR.