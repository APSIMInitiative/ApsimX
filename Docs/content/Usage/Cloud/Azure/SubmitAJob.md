---
title: "4. Submitting a Job"
draft: false
---

**1. Initialize Credentials**

This is done by creating a new Azure.Batch.Auth.BatchSharedKeyCredentials object, passing in the URL, account name, and key.

**2. Open a Batch Client**

This is used to interact with the Azure.Batch service. It may be initialised via the BatchClient.Open() method, passing in the credentials object obtained in the previous steop.

**3. Specify Pool Information**

This information is passed via an Azure.Batch.PoolInformation object. Several properties will need to be specified:

 - Maximum number of tasks per VM/compute node
 - Cloud Service Configuration, which specifies the OS of the VM
 - Timeout period for allocation of compute nodes to the pool (a TimeSpan object)
 - Desired number of dedicated VMs/Compute nodes in the pool
 - VM Size
 - Task scheduling policy

**4. Create a new Cloud Job**

This is done via the BatchClient.JobOperations.CreateJob() method, passing in the name and pool information of the job.

**5. Specify job preparation and release tasks**

The job preparation and release tasks run on all VMs/Compute nodes scheduled to run a task. The job preparation task runs before any tasks start. The job release task runs after all tasks on that node have finished.
The job preparation and release tasks are batch scripts located in ApsimX\bin\tools\jobprep.cmd and ApsimX\bin\tools\jobrelease.cmd respectively.

The job manager task runs before all other tasks, to control/manage job preparation.