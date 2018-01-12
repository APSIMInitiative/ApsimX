using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System.Security.Cryptography;
using System.IO;

namespace ApsimNG.Cloud
{
    class FileUploader
    {
        private CloudStorageAccount account;        

        public FileUploader(CloudStorageAccount acct)
        {
            account = acct;
        }

        /// <summary>
        /// Uploads a file to an Azure container.
        /// </summary>
        /// <param name="filePath">Path of the local file to be uploaded.</param>
        /// <param name="container">Container to upload the file to.</param>
        /// <param name="remoteFileName">Name of the remote file (once it has been uploaded). Name of the local file will not be changed.</param>
        /// <returns></returns>
        public string UploadFile(string filePath, string container, string remoteFileName)
        {
            CloudBlobClient blobClient = account.CreateCloudBlobClient();

            // retry connection every 3 seconds
            blobClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 10);

            CloudBlobContainer containerRef = blobClient.GetContainerReference(container);
            containerRef.CreateIfNotExists();

            CloudBlockBlob blob = containerRef.GetBlockBlobReference(remoteFileName);
            if (BlobNeedsUploading(blob, filePath))
            {
                blob.UploadFromFileAsync(filePath, FileMode.Open, new AccessCondition(), new BlobRequestOptions { ParallelOperationThreadCount = 8, StoreBlobContentMD5 = true }, null).Wait();
            }

            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(12)
            };
            return blob.Uri.AbsoluteUri + blob.GetSharedAccessSignature(policy);
        }


        private bool BlobNeedsUploading(CloudBlockBlob blob, string filePath)
        {
            if (blob.Exists())
            {
                blob.FetchAttributes();

                if (blob.Properties.ContentMD5 != null)
                {
                    string localMD5 = GetMD5(filePath);
                    if (blob.Properties.ContentMD5 == localMD5) return false;
                }                
            }
            return true;
        }

        private string GetMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    return Convert.ToBase64String(md5.ComputeHash(stream));
                }
            }
        }



    }
}
