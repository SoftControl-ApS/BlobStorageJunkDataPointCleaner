using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;
using SharedLibrary.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using static Microsoft.WindowsAzure.Storage.CloudStorageAccount;
using static SharedLibrary.ApplicationVariables;
using SharedLibrary.SunSys;

namespace SharedLibrary.Azure;

using SharedLibrary.util;
using static SharedLibrary.util.Util;

public partial class AzureBlobCtrl
{
    public string InstallationId { get; set; } = null;
    public string ContainerName { get; set; } = null;

    public AzureBlobCtrl(string containerName, string installationId)
    {
        this.ContainerName = containerName;
        this.InstallationId = installationId;
    }

    public async Task<bool> RemoveAllJunkiesDayDataPoints()
    {
        var blobClient = CreateCloudBlobClient();
        var rootContainer = blobClient.GetContainerReference(ContainerName);
        var directory = rootContainer.GetDirectoryReference(InstallationId);
        BlobContinuationToken continuationToken = null;
        Title("Remove All Junkies Day DataPoints", ConsoleColor.Cyan);
        do
        {
            var results = await directory.ListBlobsSegmentedAsync(continuationToken);
            continuationToken = results.ContinuationToken;

            var inverterStatusTasks = results.Results.Select(item => ProcessBlobAsync(item));

            await Task.WhenAll(inverterStatusTasks);
            return true;
        } while (continuationToken != null);
    }

    public async Task<string> UpDateDataPointFiles(DateOnly date, FileType fileType)
    {
        Log($"Update Production Data for {fileType}", ConsoleColor.Cyan);

        return await FixFile(fileType, date);
    }

    public async Task<bool> UpDateAllFiles(DateOnly date)
    {
        for (int year = date.Year; year >= date.Year - 2; year--)
        {
            // var date = DateOnly.FromDateTime(new DateTime(year, 1, 1));
            for (int month = 1; month <= 12; month++)
            {
                var thisDatte = DateOnly.FromDateTime(new DateTime(date.Year, month, 1));
                var dasda = await UpDateDataPointFiles(thisDatte, FileType.Month);
            }

            var adsa = await UpDateDataPointFiles(date, FileType.Year);
        }

        return true;
    }

    private async Task BackupAndReplaceOriginalFile(string fileName, string originalJson, string updatedJson)
    {
        fileName = GetFileName(fileName);
        await DeleteBlobFile($"{fileName}_BackUp");          // Backup                 
        await WriteJson(originalJson, $"{fileName}_BackUp"); // Delete original
        await DeleteBlobFile(fileName);                      // Delete original
        await WriteJson(updatedJson, fileName);              // Upload updated
    }
}