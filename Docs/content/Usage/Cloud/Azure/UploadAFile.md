---
title: "3. Upload a File to Azure Cloud Storage"
draft: false
---

Several types of files need to be uploaded in order for a job to run on Azure - model/input files, tools such as 7zip, AzCopy, the job preparation/release tasks, etc. This document describes the process of programmatically uploading a file to Azure cloud storage.

**1. Generate/Obtain storage credentials**

For your Batch account you will require a name, a URL and a key. For your storage account you will need a name and a key.
These details will be saved to `ApsimNG.Cloud.AzureSettings.Default` each time they are updated, but for the first time, they may be loaded from a .lic plain text file:

![Azure licence file format](/images/Usage.AzureLicenceFileFormat.PNG)

**2. Generate a reference to the storage account**

Instantiate an object of type `WindowsAzure.Storage.Auth.CloudStorageAccount`, passing in a `WindowsAzure.Storage.Auth.StorageCredentials` object initialised with credentials obtained in the previous step.

**3. Create a cloud blob client.**

This is a logical representation of the Azure blob storage associated with an account. It is created via the `CloudStorageAccount.CreateCloudBlobClient()` method.

**4. Generate a reference to the correct cloud storage container**

The Azure cloud storage is divided into logical containers (application, input, output, etc.). Each file must be uploaded into the appropriate container. To generate this reference, use the `CloudBlobClient.GetContainerReference()` method, passing in the appropriate container name.

An example of these steps is given below:

![Azure storage container](/images/Usage.Azure.StorageContainerGeneration.PNG)

Technically, steps 5 and 6 are optional, but they may save time (and data), and are not costly to implement.

**5. Generate a reference to a blob on Azure of the file to be uploaded**

If this is successful, then a file of the same name already exists in the container and location we are uploading to.

**6. Generate and compare MD5 hashes for the file to be uploaded and the blob found in the previous step**

The blob's MD5 is accessible through its `ContentMD5` property. If these hashes are equal, then the files are identical, and there is no need to upload the file.

**7. Upload the file**

This can be done via the `CloudBlockBlob.UploadFromFile()` or `CloudBlockBlob.UploadFromFileAsync()` methods.

[Click here](https://docs.microsoft.com/en-us/azure/batch/batch-dotnet-get-started) for more details about the file upload process.