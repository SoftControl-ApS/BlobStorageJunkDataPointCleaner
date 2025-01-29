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

namespace SharedLibrary.Azure;

using SharedLibrary.util;
using static SharedLibrary.util.Util;

public partial class AzureBlobCtrl
{
    public string InstallationId { get; set; } = null;
    public string ContainerName { get; set; } = null;

    List<CloudBlockBlob> _blobs = null;
    object lockblobs { get; } = new object();

    List<CloudBlockBlob> FetchedBlobsList
    {
        get
        {
            if (_blobs != null)
                return _blobs;

            Console.WriteLine("Error retrieving fetched blobs");
            return new List<CloudBlockBlob>();
        }
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

        string backupName = $"{fileName}_backup";
        if (fileName.Contains("pd"))
        {
            var blolb = await GetBlockBlobReference(backupName);
            try
            {
                if (!await blolb.ExistsAsync())
                {
                    await CreateAndUploadBlobFile(originalJson, backupName, source: "PUBLISH");
                }
                else
                {
                    Log("backufile: " + backupName + " Was not found");
                }

            }
            catch (Exception e )
            {
                await CreateAndUploadBlobFile(originalJson, backupName, source: "PUBLISH");
            }

        }
        return await ForcePublish(fileName, updatedJson, source: "PUBLISH");
    }

    private async Task<string> ForcePublishAndRead(string fileName, string json)
    {
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
}