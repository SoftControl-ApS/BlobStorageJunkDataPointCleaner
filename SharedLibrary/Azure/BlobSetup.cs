using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Linq;
using static SharedLibrary.ApplicationVariables;
using static Microsoft.WindowsAzure.Storage.CloudStorageAccount;

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
        public static CloudBlockBlob GetBlockBlobReference(string zip, string installationId,
          string containerName = "installations")
        {
            CloudBlobContainer rootContainer = GetContainerReference(containerName);
            CloudBlobDirectory snDir = rootContainer.GetDirectoryReference(installationId.ToString());
            CloudBlockBlob blobFile = snDir.GetBlockBlobReference(zip);
            return blobFile;
        }

        public static CloudBlobContainer GetContainerReference(string containerName = "installations")
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
