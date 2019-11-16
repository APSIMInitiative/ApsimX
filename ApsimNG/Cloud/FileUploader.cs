using System;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.RetryPolicies;
using System.Security.Cryptography;
using System.IO;

namespace ApsimNG.Cloud
{
    class FileUploader
    {
        private CloudStorageAccount account;        

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="acct">Azure storage account to upload the file to</param>
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
                blob.UploadFromFileAsync(filePath, new AccessCondition(), new BlobRequestOptions { ParallelOperationThreadCount = 8, StoreBlobContentMD5 = true }, null).Wait();
            }

            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(12)
            };
            return blob.Uri.AbsoluteUri + blob.GetSharedAccessSignature(policy);
        }

        /// <summary>
        /// Checks if a blob actually needs to be uploaded to a path.
        /// Returns false if a blob with the same MD5 already exists at the given path.
        /// Returns true otherwise.
        /// </summary>
        /// <param name="blob">Blob to be uploaded.</param>
        /// <param name="filePath">Path for the blob to be uploaded to.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the MD5 hash of a file at a given path.
        /// </summary>
        /// <param name="filePath">Path of the file.</param>
        /// <returns>MD5 of the file.</returns>
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
