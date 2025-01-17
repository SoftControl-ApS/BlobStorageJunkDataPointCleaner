using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Concurrent;
using System.Linq;
using static SharedLibrary.ApplicationVariables;
using static SharedLibrary.util.Util;
using static Microsoft.WindowsAzure.Storage.CloudStorageAccount;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
        private List<CloudBlockBlob> blobBLocks;

        public static CloudBlobClient CreateCloudBlobClient()
        {
            CloudStorageAccount storageAccount = Parse(AzureBlobConnectionString);
            CloudBlobClient? blobClient = storageAccount.CreateCloudBlobClient();
            return blobClient;
        }

        private async Task<List<CloudBlockBlob>> GetAllBlobsAsync(string containerName = "installations")
        {
            var snDir = GetContainerReference(containerName).GetDirectoryReference(InstallationId);
            BlobContinuationToken continuationToken = null;
            var blobs = new List<CloudBlockBlob>();

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

        public async Task<CloudBlockBlob> GetBlockBlobReference(string zip, string installationId,
                                                                string containerName = "installations")
        {
            if (blobBLocks == null || !blobBLocks.Any())
                blobBLocks = await GetAllBlobsAsync();
            CloudBlockBlob blobFile = blobBLocks.FirstOrDefault(b => b.Name == $"{installationId}/{zip}");
            if (blobFile == null)
            {
                LogError($"Blob '{zip}' in installation '{installationId}' does not exist.");
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