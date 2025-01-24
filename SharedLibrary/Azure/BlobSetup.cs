using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Concurrent;
using System.Linq;
using static SharedLibrary.ApplicationVariables;
using static SharedLibrary.util.Util;
using static Microsoft.WindowsAzure.Storage.CloudStorageAccount;
using SharedLibrary.SunSys;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Threading;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
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

                blobs = resultSegment.Results.OfType<CloudBlockBlob>().ToList();
            } while (continuationToken != null);

            return blobs;
        }

        public async Task<CloudBlockBlob?> GetBlockBlobReference(string zip, string containerName = "installations")
        {
            var blocks = await GetAllBlobsAsync();
            CloudBlockBlob? blobFile = blocks.FirstOrDefault(b => b.Name == $"{InstallationId}/{zip}");
            if (blobFile == null)
            {
                if (blobFile == null && zip.Contains("pd"))
                {
                    await GenerateAndUploadEmptyDayFile(ExtractDateFromFileName(zip));
                    blobFile = blocks.FirstOrDefault(b => b.Name == $"{InstallationId}/{zip}");
                    return blobFile;
                }

                LogError($"Blob '{zip}' in installation '{InstallationId}' does not exist.");
                SharedLibrary.ApplicationVariables.FailedFiles.Add(new(zip, "GetBlockBlobReference() x2"));
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