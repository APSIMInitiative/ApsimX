---
title: "Job Submission"
draft: false
---

There are several options available when submitting a job to the cloud. This document gives a brief description of each.

**1. Set/obtain credentials**

If this is your first time submitting a job to the cloud, you will need to provide credentials. For details

**1. Job Description/Name**

This is a name or description of the job. It does not need to be unique. The default name is the name of the currently selected node in the left-hand panel.


**2. Number of CPU Cores**

This option specifies the maximum number of CPU cores (and, therefore, VMs) that your job should use. The increments are equal to the number of cores per VM. 
For example, if, under your licence, each VM has 16 cores, then the options available here will be 16, 32, 48, etc. Selecting 48 cores means the job would (in this case) use 3 VMs.
For smaller jobs it may be faster and cheaper to use less cores, as there is an overhead associated with each VM starting and finishing. 


**3. Save model files**

Before your job is uploaded to the cloud, some model files are generated. Normally these model files are temporary, and are deleted after they are uploaded. If this option is selected however, the model files will be saved to the specified directory.


**4. Use APSIM Next Generation from a directory**

This is one of the two ways to specify the version of APSIM to be run on the cloud. It allows you to specify a directory containing APSIM to be uploaded (e.g. C:\Hobbies\ApsimX).


**5. Use a zipped version of APSIM Next Generation**

This is the second way to specify the version of APSIM to be run on the cloud. It allows you to specify a zip file containing APSIM to be uploaded (e.g. C:\Hobbies\ApsimX.zip).


**6. Send email upon completion**

If this option is checked, an email will be sent to the address provided when the job has finished running.


**7. Automatically download results once complete**

Functionality for this control is currently not implemented (as of 18/1/18). The idea is that when the job finishes, APSIM will download the results.


**8. Summarise Results**

Functionality for this control is currently not implemented (as of 18/1/18). The idea is that when the results are auto-downloaded, they will automatically combined into a single .csv file. This is a legacy option from MARS, but in the APSIM implementation it may turn out to be more useful to import the results directly into the node's DataStore instead of generating a .csv file.


**9. Output Directory**

The directory that results will be downloaded to. Results can only be downloaded via the job viewer (as of 18/1/18), but APSIM will (once the OK button is clicked) remember the path selected here, and set this as the default path in the job viewer's download dialog. 


**10. OK Button**

Clicking this button will cause the job to be submitted. If any of the given directories or zip files don't exist, an error will be displayed and the job will not be submitted. Any currently selected settings on the form will be saved (except for the job name) and set as the defaults the next time the job submission form is opened. 


**11. Cancel Button**

This button closes the job submission form. It does not cancel any submission in progress. 