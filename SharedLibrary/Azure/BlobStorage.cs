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

    private List<CloudBlockBlob> _cloudBlobs { get; set; }

    public List<CloudBlockBlob> CloudBlobs
    {
        get
        {
            if (_cloudBlobs == null || !_cloudBlobs.Any())
            {
                intiBlobs().ConfigureAwait(false);
                return _cloudBlobs;
            }
            else
                return _cloudBlobs;
        }
        set { _cloudBlobs = value; }
    }

    async Task intiBlobs()
    {
        _ = GetAllBlobsAsync().Result;
    }

    public DateTime LastUpLoadDateTime { get; set; }
    public DateTime LastDownloadLoadDateTime { get; set; }
    object lockblobs { get; } = new object();

    CloudBlobDirectory _installationDirectory = null;

    public AzureBlobCtrl(string containerName, string installationId)
    {
        this.ContainerName = containerName;
        this.InstallationId = installationId;
        LastDownloadLoadDateTime = DateTime.Now;
        LastUpLoadDateTime = DateTime.Now;
        intiBlobs().ConfigureAwait(false);
    }

    private async Task<bool> BackupAndReplaceOriginalFile(string fileName, string? originalJson, string updatedJson)
    {
        fileName = GetFileName(fileName);

        string backupName = $"{fileName}_backup";
        if (fileName.Contains("pd"))
        {
            try
            {
                var fileexist = !await BlobExistsAsync(backupName);
                if (!fileexist)
                {
                    await ForcePublish($"{backupName}", originalJson, isPd: true);
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        return await ForcePublish(fileName, updatedJson);
    }


    private async Task<bool> BlobExistsAsync(string blobName)
    {
        var blob = InstallationContainerReference.GetBlockBlobReference(blobName + ".zip");
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