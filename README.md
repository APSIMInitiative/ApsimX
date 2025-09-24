# ApsimX

ApsimX is the next generation of [APSIM](https://www.apsim.info)

* APSIM is an agricultural modelling framework used extensively worldwide.
* It can simulate a wide range of agricultural systems.
* It begins its third decade evolving into an agro-ecosystem framework.

## Licencing Conditions

Use of APSIM source code is provided under the terms and conditions provided by either the General Use Licence or the Special Use Licence.  Use in any way is not permitted unless previously agreed to and currently bound by a licence agreement which can be reviewed on [https://www.apsim.info/](https://www.apsim.info/). The General Use licence can be found [here](https://www.apsim.info/wp-content/uploads/2023/09/APSIM_General_Use_Licence.pdf). The Special Use licence can be found [here](https://www.apsim.info/wp-content/uploads/2023/09/APSIM_Special_Use_Licence.pdf)
Any questions, please email [apsim@csiro.au](mailto:apsim@csiro.au?subject=Licence%20Enquiry).

## Getting Started

**Hardware required**:

Any recent PC with a minimum of 8Gb of RAM.

**Software required**:

64-bit version of Microsoft Windows 10, Windows 11, Linux or macOS.

### Installation

Binary releases are available via our [registration system](https://registration.apsim.info).

## Contributing

Any individual or organisation (a 3rd party outside of the APSIM Initiative (AI)) who uses APSIM must be licensed do so by the APSIM Initiative. On download of APSIM, the terms and conditions of a General Use Licence are agreed to and binds the user.

Intellectual property rights in APSIM are retained by the APSIM Initiative. If a licensee makes any improvements to APSIM, the intellectual property rights to those improvements belong to the APSIM Initiative. This means that the APSIM Initiative can choose to make the improvements - including source code - and these improvements would then be made available to all licensed users. As part of the submission process, you are complying with this term as well as making it available to all licensed users. Any Improvements to APSIM are required to be unencumbered and the contributing party warrants that the IP being contributed does not and will not infringe any third party IPR rights.

Please read on below and review additional information in our [guide](https://apsimnextgeneration.netlify.app/contribute/).

## Software Development Guidelines, Rules and Processes

### General Good Practices

#### Things to do :white_check_mark:

* As the project follows the agile methodology, short-lived (1-2 weeks max) feature branches are ideal. Branches that are used to develop a new plant are an exception here.
* Aim to make smaller and more frequent PRs over larger less frequent PRs. Smaller PRs tend to be easier to review. Larger pieces of work can usually be broken into smaller feature branches which are easier to manage.
* Regularly pull in changes from APSIMInitiative/master branch.

#### Things to avoid :x:

* Keeping branches in development for long periods of time. Branches that have been left and need to be brought up to date with the newest changes can be difficult to update.
* Submitting pull requests that resolve multiple unrelated issues in the one pull request. Doing so increases the difficulty of review.
* Submitting pull requests that contain multiple new features. This also increases the review time.

### Reporting and creating issues

#### Best practices

* When submitting an issue include the apsim file with any required met and input files required to make it run in a zip archive.
* Describe the steps that lead to the issue occurring so that it can be reproduced. This includes details like where you clicked, what settings were enabled and what operating system it occurred on.
* For graphical user interface (GUI) issues please submit a minimally reproducible example apsim file, describe the user interface model this issue affects and the steps to reproduce the error.
* Describe issues with an emphasis on the "Why" more than "What" or "How".
* Submit issues before you begin creating a solution. This has the advantage of avoiding unnecessary effort if the issue is found to be resolved or in the process of being resolved and allows others to suggest possible approaches and considerations that can improve the subsequent PR.

### Pull requests and code submissions

#### Pull Request Priorities

* Bug fix PRs that stop APSIM users from being able to use the software are top priority. This is in line with the software teams' zero bug policy.
* Other bugs that only effect a portion of the software are the next highest priority.
* New features are then prioritized.

#### Getting a Pull request reviewed

* To have the software team review a pull request attach the `Ready for Software Review` label.
* It is best practice to rerun a pull request if it is behind the main branch. This ensures it runs with the newest changes.

### Pull request requirements and best practices

* Each pull request must only do one thing.
* The requirements for a pull request differ based on the changes submitted.
  * **Science** changes require observed data to demonstrate that the change does what it intends. Additionally unit tests are to be included.
  * **Graphical user interface (GUI)** changes and fixes that include a short video showing the changes working will improve review time.
  * **Bug fixes** should include a unit test to reduce the likelihood of recurrence and to also verify the fix.
* All pull requests should briefly and concisely describe what the issue was, what changes have been made and the rationale.

#### Adding new simulations and apsim files

##### Making changes to existing datasets

* To enable a fast and thorough review of changes when new files and data are added, it is best to first create a pull request that only adds the data followed by another pull request that adds the apsim file. This is to allow a clearer review that allows reviewers to determine which files changed statistics and helps with debugging when required.

### Pull request process

1. A pull request is submitted with a `Ready for Software Review` label. If you are unable to apply labels, request to be added to the developers github team by submitting a comment in the pull request.
2. Validation and user tests are run.
3. Once status checks all run successfully, a peer review is performed by the software team.
4. The `High level Review` will be applied while the PR is reviewed from a high level. Details on what a high level review entails are available under the [high level review details section](#high-level-review)
4. If issues are found or need further discussion the `Ready for Software Review` will be removed and the `More information needed` label will be applied to the PR.
5. Once the issues have been resolved and discussion is complete the pull request author should reapply the `Ready for Software Review` label.
6. Once reviewed the pull request will be merged.

### Changing apsimx files

* When fixing an issue with `.apsimx` files avoid opening files and making changes directly. Instead create a converter so that all affected `.apsimx` files will be changed automatically. Additionally, resource files (models loaded from json files) will need to be changed and this is best done by using the `update resources` button in the main menu.

### Review details

#### High level review

## Publications

* [doi:10.1016/j.envsoft.2014.07.009](https://dx.doi.org/10.1016/j.envsoft.2014.07.009)
