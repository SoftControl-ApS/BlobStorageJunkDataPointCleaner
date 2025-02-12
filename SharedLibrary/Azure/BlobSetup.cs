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

    private async Task<List<CloudBlockBlob>>? GetAllBlobsAsync()
    {
        if(_installationDirectory != null)
        _installationDirectory = GetContainerReference(ContainerName).GetDirectoryReference(InstallationId);
        // var cloudBlobDirectory = InstallationContainerReference.GetDirectoryReference(InstallationId);

        if(LastDownloadLoadDateTime <= LastUpLoadDateTime)
        {
            cloudBlobs = new();
            BlobContinuationToken? continuationToken = null;
            do
            {
                var resultSegment = await _installationDirectory.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = resultSegment.ContinuationToken;
                cloudBlobs = resultSegment.Results.OfType<CloudBlockBlob>().ToList();
            } while (continuationToken != null);
            LastDownloadLoadDateTime = DateTime.Now;
        }
        return cloudBlobs;
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

    public CloudBlobContainer InstallationContainerReference
    {
        get
        {
            var blobClient = CreateCloudBlobClient();
            var _cloudBlobContainer = blobClient.GetContainerReference("installations");
            _cloudBlobContainer.SetPermissionsAsync(
                new BlobContainerPermissions()
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                }
            );
            return _cloudBlobContainer;
        }
    }
}