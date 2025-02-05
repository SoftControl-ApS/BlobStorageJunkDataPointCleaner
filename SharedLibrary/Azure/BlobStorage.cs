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

    CloudBlobDirectory _installationDirectory = null;

    public AzureBlobCtrl(string containerName, string installationId)
    {
        this.ContainerName = containerName;
        this.InstallationId = installationId;
    }

    private async Task<bool> BackupAndReplaceOriginalFile(string fileName, string? originalJson, string updatedJson)
    {
        fileName = GetFileName(fileName);

        string backupName = $"{fileName}_backup";
        if (fileName.Contains("pd"))
        {
            try
            {
                if (!await BlobExistsAsync(backupName))
                {
                    await ForcePublish($"{backupName}", originalJson, isPd: true);
                }
            }
            catch (Exception e)
            {
                await ForcePublish($"{backupName}", originalJson, isPd: true);
                return true;
            }
        }

        return await ForcePublish(fileName, updatedJson);
    }


    private async Task<bool> BlobExistsAsync(string blobName)
    {
        var blob = _cloudBlobContainer.GetBlockBlobReference(blobName);
        try
        {
            if (blob != null)
                return await blob.ExistsAsync();
            return false;
        }
        catch (Exception e)
        {
            // Noncompliant: is the block empty on purpose, or is code missing?
        }

        return false;
    }


    private async Task<string> ForcePublishAndRead(string fileName, string json)
    {
        fileName = GetFileName(fileName);
        if (await BlobExistsAsync(fileName))
        {
            var deleted = await DeleteBlobFile(fileName);
        }

        var created = await CreateAndUploadBlobFile(json, fileName);

        if (created)
        {
            var read = await ReadBlobFile(fileName);
            return read;
        }
        else
        {
            LogError("Could Not UPLOAD FILE: " + fileName);
            Console.ReadLine();
            return null;
        }
    }

    private async Task<bool> ForcePublish(string fileName, string json, bool isPd = false)
    {
        fileName = GetFileName(fileName);
        if (await BlobExistsAsync(fileName))
        {
            var deleted = await DeleteBlobFile(fileName);
        }
        return await CreateAndUploadBlobFile(json, fileName, isPd: isPd);
    }
}