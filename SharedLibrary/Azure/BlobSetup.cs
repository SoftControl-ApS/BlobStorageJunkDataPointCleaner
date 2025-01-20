using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Concurrent;
using System.Linq;
using static SharedLibrary.ApplicationVariables;
using static SharedLibrary.util.Util;
using static Microsoft.WindowsAzure.Storage.CloudStorageAccount;
using SharedLibrary.SunSys;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
        private ConcurrentBag<CloudBlockBlob> blobBLocks;

        private async Task<ConcurrentBag<CloudBlockBlob>> initBlobBlocks()
        {
            blobBLocks = await GetAllBlobsAsync();

            return blobBLocks;
        }

        public static CloudBlobClient CreateCloudBlobClient()
        {
            CloudStorageAccount storageAccount = Parse(AzureBlobConnectionString);
            CloudBlobClient? blobClient = storageAccount.CreateCloudBlobClient();
            return blobClient;
        }

        private async Task<ConcurrentBag<CloudBlockBlob>> GetAllBlobsAsync(string containerName = "installations")
        {
            var snDir = GetContainerReference(containerName).GetDirectoryReference(InstallationId);
            BlobContinuationToken continuationToken = null;
             var blobs = new ConcurrentBag<CloudBlockBlob>();

            do
            {
                var resultSegment = await snDir.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = resultSegment.ContinuationToken;

                foreach (var item in resultSegment.Results)
                {
                    if (item is CloudBlockBlob blob)
                    {
                        blobs.Add(blob);
                    }
                }
            } while (continuationToken != null);

            return blobs;
        }

        public async Task<CloudBlockBlob> GetBlockBlobReference(string zip, string containerName = "installations")
        {
            if (blobBLocks == null || !blobBLocks.Any())
            {
                blobBLocks = await GetAllBlobsAsync();
            }

            CloudBlockBlob blobFile = blobBLocks.FirstOrDefault(b => b.Name == $"{InstallationId}/{zip}");
            if (blobFile == null)
            {
                LogError($"Blob '{zip}' in installation '{InstallationId}' does not exist.");
            }

            return blobFile;
        }

        public CloudBlobContainer GetContainerReference(string containerName = "installations")
        {
            var blobClient = CreateCloudBlobClient();
            CloudBlobContainer rootContainer = blobClient.GetContainerReference(containerName);
            rootContainer.SetPermissionsAsync(
                new BlobContainerPermissions()
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                }
            );
            return rootContainer;
        }
    }
}