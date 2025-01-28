using System.Collections.Concurrent;
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

    List<CloudBlockBlob> _blobs = null;
    object lockblobs { get; } = new object();
    List<CloudBlockBlob> blobs
    {
        get
        {
            if (_blobs == null)
            {
                _blobs = GetAllBlobsAsync().Result;
            }

            return _blobs;
        }
    }

    async Task<bool> RefreshBlobs()
    {

        _blobs = GetAllBlobsAsync().Result;

        if (_blobs != null)
        {
            return true;
        }

        return false;
    }


    public AzureBlobCtrl(string containerName, string installationId)
    {
        this.ContainerName = containerName;
        this.InstallationId = installationId;
    }

    private async Task<bool> BackupAndReplaceOriginalFile(string fileName, string? originalJson, string updatedJson)
    {
        if (updatedJson == null)
        {
            LogError("updated Json is null");
            return false;
        }

        fileName = GetFileName(fileName);

        if (fileName.Contains("pd"))
            await ForcePublish($"{fileName}_BackUp", originalJson, source: "PUBLISH");
        return await ForcePublish(fileName, updatedJson, source: "PUBLISH");
    }

    private async Task<string> ForcePublishAndRead(string fileName, string json)
    {
        if (!IsValidJson(json))
        {
            LogError("Invalid updated Json is null");
            return null;
        }

        fileName = GetFileName(fileName);
        await DeleteBlobFileIfExist(fileName);
        await CreateAndUploadBlobFile(json, fileName);
        return await ReadBlobFile(fileName);
    }

    private async Task<bool> ForcePublish(string fileName, string json, string source = "PUBLISH")
    {
        if (!IsValidJson(json))
        {
            LogError("Publish Failed: Invalid Json: " + json.ToString());
            return false;
        }

        fileName = GetFileName(fileName);
        await DeleteBlobFileIfExist(fileName);
        return await CreateAndUploadBlobFile(json, fileName, source: source);
    }

    #region TDO

    //public async Task<bool> UpDateAllFiles(DateOnly date)
    //{
    //    var yearsResult = new ConcurrentDictionary<DateOnly, string>();
    //    var monthsResult = new ConcurrentDictionary<DateOnly, string>();
    //    try
    //    {
    //        for (int year = date.Year; year >= date.Year - 2; year--)
    //        {
    //            for (int month = 1; month <= 12; month++)
    //            {
    //                var thisDatte = DateOnly.FromDateTime(new DateTime(date.Year, month, 1));
    //                monthsResult.TryAdd(thisDatte, await UpDateDataPointFiles(thisDatte, FileType.Month));
    //            }

    //            yearsResult.TryAdd(date, await UpDateDataPointFiles(date, FileType.Year));
    //        }

    //        if (yearsResult.ToList().Any(res => res.Value != "null"))
    //        {
    //            LogError($"Some data points were not reachable");


    //            var total = await UpDateDataPointFiles(date, FileType.Total);
    //        }

    //        return true;
    //    }
    //    catch (Exception ex)
    //    {
    //        LogError($"Error updating data: {ex.Message}");
    //        return false;
    //    }
    //}

    //public async Task<string> UpDateDataPointFiles(DateOnly date, FileType fileType)
    //{
    //    Title($"Update Production Data for {fileType}", ConsoleColor.Cyan);

    //    return await FixFile(fileType, date);
    //}

    #endregion
}