using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using SharedLibrary.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using static Microsoft.WindowsAzure.Storage.CloudStorageAccount;
using static SharedLibrary.ApplicationVariables;
using SharedLibrary.SunSys;
namespace SharedLibrary.Azure;

using SharedLibrary.util;
using static SharedLibrary.util.Logging;

public partial class AzureBlobCtrl
{
    public string InstallationId { get; set; } = null;
    public string ContainerName { get; set; } = null;

    public static List<InverterModeAPI> invModels;
    public static List<InverterAPI> inverters;


    public AzureBlobCtrl(string containerName, string installationId)
    {
        this.ContainerName = containerName;
        this.InstallationId = installationId;
    }

    public async Task<List<string>> RemoveJunkieDataPoints()
    {
        var blobClient = CreateCloudBlobClient();
        var rootContainer = blobClient.GetContainerReference(ContainerName);
        var directory = rootContainer.GetDirectoryReference(InstallationId);
        BlobContinuationToken continuationToken = null;
        var fileList = new List<string>();
        Log("Filer Indhold", ConsoleColor.Cyan);

        do
        {
            var results = await directory.ListBlobsSegmentedAsync(continuationToken);
            continuationToken = results.ContinuationToken;

            var inverterStatusTasks = results.Results.Select(item => ProcessBlobAsync(item, fileList, Convert.ToInt32(InstallationId)));

            await Task.WhenAll(inverterStatusTasks);
        } while (continuationToken != null);

        return fileList;
    }
}