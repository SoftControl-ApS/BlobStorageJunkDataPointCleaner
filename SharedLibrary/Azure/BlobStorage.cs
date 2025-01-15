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

public partial class AzureBlobCtrl
{
    private static string _sn = "";

    public static List<InverterModeAPI> invModels;
    public static List<InverterAPI> inverters;
    public static async Task<List<string>> GetFilesFromFolderAsync(string containerName, string sn)
    {
        _sn = sn;

        var blobClient = CreateCloudBlobClient();
        var rootContainer = blobClient.GetContainerReference(containerName);
        var directory = rootContainer.GetDirectoryReference(sn);
        BlobContinuationToken continuationToken = null;
        var fileList = new List<string>();
        Log("Filer Indhold", ConsoleColor.Cyan);

        do
        {
            var results = await directory.ListBlobsSegmentedAsync(continuationToken);
            continuationToken = results.ContinuationToken;

            var inverterStatusTasks = results.Results.Select(item => ProcessBlobAsync(item, fileList, Convert.ToInt32(sn)));

            await Task.WhenAll(inverterStatusTasks);
        } while (continuationToken != null);

        return fileList;
    }
}