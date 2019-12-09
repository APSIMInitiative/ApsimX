using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Storage.Blob;

namespace ApsimNG.Cloud
{
    public static class ApsimJobManagerExtension
    {
        public static JobManagerTask ToJobManagerTask(this APSIMJob job, Guid jobId, CloudBlobClient blobClient, bool shouldSubmitTasks, bool autoScale)
        {
            var cmd = string.Format("cmd.exe /c {0} job-manager {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                BatchConstants.GetJobManagerPath(jobId),
                job.BatchAuth.Url,
                job.BatchAuth.Account,
                job.BatchAuth.Key,
                job.StorageAuth.Account,
                job.StorageAuth.Key,
                jobId,
                BatchConstants.GetModelPath(jobId),
                shouldSubmitTasks,
                autoScale
            );

            return new JobManagerTask
            {
                CommandLine = cmd,
                DisplayName = "Job manager task",
                KillJobOnCompletion = true,
                Id = BatchConstants.JobManagerName,
                RunExclusive = false,
                ResourceFiles = GetResourceFiles(job, blobClient).ToList()
            };
        }

        private static IEnumerable<ResourceFile> GetResourceFiles(APSIMJob job, CloudBlobClient blobClient)
        {
            var toolsRef = blobClient.GetContainerReference("jobmanager");
            foreach (CloudBlockBlob listBlobItem in toolsRef.ListBlobs())
            {
                var sas = listBlobItem.GetSharedAccessSignature(new SharedAccessBlobPolicy
                {
                    SharedAccessStartTime = DateTime.UtcNow.AddHours(-1),
                    SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(2),
                    Permissions = SharedAccessBlobPermissions.Read,
                });
                yield return ResourceFile.FromUrl(listBlobItem.Uri.AbsoluteUri + sas, listBlobItem.Name);
            }
        }
    }
}
