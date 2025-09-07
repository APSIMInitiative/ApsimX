# ApsimX

[![Netlify Status](https://api.netlify.com/api/v1/badges/aff3e00e-23f5-41e7-a721-bc9a171b3199/deploy-status)](https://app.netlify.com/sites/apsimnextgeneration/deploys)
[![Build Status](https://jenkins.apsim.info/buildStatus/icon?job=apsim)](https://jenkins.apsim.info/user/hol353/my-views/view/all/job/apsim/)

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

Please read our [guide](https://apsimnextgeneration.netlify.app/contribute/).

## Software Development Guidelines, Rules and processes

### Reporting issues

#### Best practices

* When submitting an issue include the apsim file with any required met and input files required to make it run in a zip archive.
* Describe the steps that lead to the issue occuring so that it can be reproduced. This includes details like where you clicked, what settings were enabled and what operating system it occurred on.
* For graphical user interace (GUI) issues please submit a minimally reproducable example apsim file, describe the user interface model this issue affects

### Pull requests and code submissions

#### Getting a Pull request reviewed

* To have the software team review a pull request attach the `Ready for Software Review` label.
* It is best practice to rerun a pull request if it is behind the main branch. This ensures it runs with the newest changes.

### Pull request requirements and best practices

* Each pull request must only do one thing.
* The requirements for a pull request differ based on the changes submitted.
  * **Science** changes require observed data to demonstrate that the change does what it intends. Additionally unit test are to be included.
  * **Graphical user interface (GUI)** changes and fixes that include a short video showing the changes working will improve review time.
  * **Bug fixes** must include a unit test to reduce the likelihood of recurrence and to also verify the fix.
* All pull requests should briefly and concisely describe what the issue was, what changes have been made and the rationale.

#### Adding new simulations and apsim files

##### Making changes to existing datasets

* To enable a fast and thorough review of changes when new files and data are added, it is best to first create a pull request that only adds the data. Followed by another pull request that adds the apsim file. This is to allow a clearer review that allows reviewers to determine which files changed statistics and helps with debugging when required.

### Pull request process

1. A pull request is submitted with a `Ready for Software Review` label. If you are unable to apply labels, request to be added to the developers github team.
2. Validation and user tests are run.
3. Once status checks all run successfully, a peer review performed by the software team.
4. If issues are found or need further discussion the `Ready for Software Review` will be removed.
5. Once the issues have been resolved and discussion is complete the pull request author should reapply the `Ready for Software Review` label.
6. Once reviewed the pull request will be merged.

## Publications

* [doi:10.1016/j.envsoft.2014.07.009](https://dx.doi.org/10.1016/j.envsoft.2014.07.009)
