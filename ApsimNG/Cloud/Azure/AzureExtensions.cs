using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsimNG.Cloud.Azure
{
    public static class AzureExtensions
    {
        public static async Task<List<CloudBlockBlob>> ListBlobsAsync(this CloudBlobContainer container)
        {
            BlobContinuationToken continuationToken = null;
            List<CloudBlockBlob> results = new List<CloudBlockBlob>();
            do
            {
                var response = await container.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results.Cast<CloudBlockBlob>());
            }
            while (continuationToken != null);

            return results;
        }
    }
}