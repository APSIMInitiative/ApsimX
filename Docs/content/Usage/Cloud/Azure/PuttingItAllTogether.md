---
title: "5. Putting it all together"
draft: false
---

The following steps describe the process of creating and submitting a job.

**1. Save User Input**

User Inputs such as the APSIM directory, number of CPU cores, etc. are saved to `ApsimNG.Properties.Settings.Default`. The data stored here persists between APSIM sessions, so the next time the user goes to submit a job, most of the input fields will be populated with their choices from this time.

**2. Upload Tools**

Tools such as 7zip, AzCopy, CMail, the job manager, etc. need to be uploaded to the tools container. This container is shared between all jobs running under your Azure batch/storage licence, and the tools are not deleted from cloud storage after the job finishes. This means that if a previous job has uploaded the tools, yours will not need to. The local tools directory is located at ApsimX\bin\tools. [See here for more details.](/usage/cloud/azure/uploadafile)

**3. Upload Model**

The model comes from the context of the node that the user right-clicked on in order to open the job submission form. Each simulation is serialised into XML and written to an .apsimx file. These files are then zipped and uploaded.

**4. Create and Submit an Azure Batch Job**

Information on how to do this is provided [here](/usage/cloud/azure/submitajob)