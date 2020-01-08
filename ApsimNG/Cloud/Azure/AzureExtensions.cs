using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApsimNG.Cloud.Azure
{
    public static class AzureExtensions
    {
        /// <summary>
        /// List all blobs in a cloud storage container.
        /// </summary>
        /// <param name="container">Container whose blobs should be enumerated.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task<List<CloudBlockBlob>> ListBlobsAsync(this CloudBlobContainer container, CancellationToken ct)
        {
            BlobContinuationToken continuationToken = null;
            List<CloudBlockBlob> results = new List<CloudBlockBlob>();
            do
            {
                var response = await container.ListBlobsSegmentedAsync(continuationToken, ct);
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results.Cast<CloudBlockBlob>());

                if (ct.IsCancellationRequested)
                    return null;
            }
            while (continuationToken != null);

            return results;
        }
    }
}
