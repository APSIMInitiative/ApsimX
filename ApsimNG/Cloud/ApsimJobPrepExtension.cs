using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Storage.Blob;

namespace ApsimNG.Cloud
{
    public static class ApsimJobPrepExtension
    {
        public static JobPreparationTask ToJobPreparationTask(this APSIMJob job, Guid jobId, CloudBlobClient blobClient)
        {
            return new JobPreparationTask
            {
                CommandLine = "cmd.exe /c jobprep.cmd",
                ResourceFiles = GetResourceFiles(job, blobClient).ToList(),
                WaitForSuccess = true
            };
        }

        public static JobReleaseTask ToJobReleaseTask(this APSIMJob job, Guid jobId, CloudBlobClient blobClient)
        {
            return new JobReleaseTask
            {
                CommandLine = "cmd.exe /c jobrelease.cmd",
                ResourceFiles = GetResourceFiles(job, blobClient).ToList(),
                EnvironmentSettings = new[]
                {
                    new EnvironmentSetting("APSIM_STORAGE_ACCOUNT", job.StorageAuth.Account),
                    new EnvironmentSetting("APSIM_STORAGE_KEY", job.StorageAuth.Key),
                    new EnvironmentSetting("JOBNAME", job.DisplayName),
                    new EnvironmentSetting("RECIPIENT", job.Recipient)
                }
            };
        }

        /// <summary>
        /// Returns the zipped Apsim file and helpers like AzCopy and 7zip
        /// </summary>
        /// <param name="job"></param>
        /// <param name="blobClient"></param>
        /// <returns></returns>
        private static IEnumerable<ResourceFile> GetResourceFiles(APSIMJob job, CloudBlobClient blobClient)
        {
            yield return ResourceFile.FromUrl(job.ModelZipFileSas, BatchConstants.ModelZipFileName);

            var toolsRef = blobClient.GetContainerReference("tools");
            foreach(CloudBlockBlob listBlobItem in toolsRef.ListBlobs())
            {
                var sas = listBlobItem.GetSharedAccessSignature(new SharedAccessBlobPolicy
                {
                    SharedAccessStartTime = DateTime.UtcNow.AddHours(-1),
                    SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(2),
                    Permissions = SharedAccessBlobPermissions.Read,
                });
                yield return ResourceFile.FromUrl(listBlobItem.Uri.AbsoluteUri + sas, listBlobItem.Name);
            }

            var apsimRef = blobClient.GetContainerReference("apsim");
            foreach (CloudBlockBlob listBlobItem in apsimRef.ListBlobs())
            {
                if (listBlobItem.Name.ToLower().Contains(job.ApsimApplicationPackageVersion.ToLower()))
                {
                    var sas = listBlobItem.GetSharedAccessSignature(new SharedAccessBlobPolicy
                    {
                        SharedAccessStartTime = DateTime.UtcNow.AddHours(-1),
                        SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(2),
                        Permissions = SharedAccessBlobPermissions.Read
                    });
                    yield return ResourceFile.FromUrl(listBlobItem.Uri.AbsoluteUri + sas, listBlobItem.Name);
                }
            }
        }
    }
}
