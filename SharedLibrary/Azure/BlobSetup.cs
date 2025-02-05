using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Concurrent;
using System.Linq;
using static SharedLibrary.ApplicationVariables;
using static SharedLibrary.util.Util;
using static Microsoft.WindowsAzure.Storage.CloudStorageAccount;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Threading;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl // PR: partial class sucks. Don't bother to change it. Just wanted to complain
{
    public static CloudBlobClient CreateCloudBlobClient()
    {
        CloudStorageAccount storageAccount = Parse(AzureBlobConnectionString);
        //Log("new Client", ConsoleColor.Yellow);
        Console.ResetColor();
        CloudBlobClient? blobClient = storageAccount.CreateCloudBlobClient();
        return blobClient;
    }

    private async Task<List<CloudBlockBlob>> GetAllBlobsAsync(string containerName = "installations")
    {
        // _installationDirectory = GetContainerReference(containerName).GetDirectoryReference(InstallationId);
        var cloudBlobDirectory = InstallationContainerReference.GetDirectoryReference(InstallationId);

        BlobContinuationToken? continuationToken = null;
        do
        {
            var resultSegment = await cloudBlobDirectory.ListBlobsSegmentedAsync(continuationToken);
            continuationToken = resultSegment.ContinuationToken;

            lock (lockblobs)
            {
                _blobs = resultSegment.Results.OfType<CloudBlockBlob>().ToList();
            }
        } while (continuationToken != null);

        return _blobs;
    }

    public async Task<CloudBlockBlob> GetBlockBlobReference(string zip)
    {
        var blobFile = (await GetAllBlobsAsync()).FirstOrDefault(b => b.Name == $"{InstallationId}/{zip}");
        if (blobFile == null)
        {
            LogError($"Blob '{zip}' in installation '{InstallationId}' does not exist.");
        }

        return blobFile;
    }

    // public CloudBlobContainer GetContainerReference(string containerName = "installations")
    // {
    //     var blobClient = CreateCloudBlobClient();
    //     CloudBlobContainer rootContainer = blobClient.GetContainerReference(containerName);
    //     rootContainer.SetPermissionsAsync(
    //         new BlobContainerPermissions()
    //         {
    //             PublicAccess = BlobContainerPublicAccessType.Blob
    //         }
    //     );
    //     return rootContainer;
    // }
    private CloudBlobContainer _cloudBlobContainer = null;

    public CloudBlobContainer InstallationContainerReference
    {
        get
        {
            if (_cloudBlobContainer == null)
            {
                var blobClient = CreateCloudBlobClient();
                _cloudBlobContainer = blobClient.GetContainerReference("installations");
                _cloudBlobContainer.SetPermissionsAsync(
                    new BlobContainerPermissions()
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    }
                );
                return _cloudBlobContainer;
            }
            else
            {
                return _cloudBlobContainer;
            }
        }
    }
}