---
title: "Science"
draft: false
weight: 20
---

For changes to be accepted by the APSIM Initiaive Reference Panel submissions must adhere to software and science guidelines. 

All submissions that contain major changes to model science e.g. new models or new processes (labeled as 'Major' in GitHub) will undergo peer-review by at least one independent reviewer. The Reference Panel will manage this process. The science reviewer(s) are responsible for ensuring all science guildlines have been met. 

Reasons need to be given by the model author should any of the guidelines not be followed.

**Submission Guidelines**

* Submissions will be via a [GitHub Pull Request](/contribute/sourcetree/pushandpullrequest)

* For science submissions (new models or processes), the submission pull request will have all files (.apsimx, .met, .xlsx) in a directory formatted as Tests\UnderReview\MODELNAME. The directory can contain:
	- weather files (*.met)
	- observed files (*.xlsx)
	- MODELNAME.apsimx (validation simulations)
	- MODELNAME Example.apsimx (example simulations)
	
* If a new model has been submitted, it will be under a *Replacements* node in the MODELNAME.apsimx and MODELNAME Example.apsimx files.

**Science Guidelines**

* [Testing] (/development/science/testing)

* [Documentation] (/development/science/documentation)

* [Examples] (/development/science/examples)