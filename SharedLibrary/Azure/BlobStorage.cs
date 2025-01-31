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
            // PR: If this is true the it will recursively call itself until the stack overflows and the program crashes.
            // Maybe just log the error and return null? Or limit the number of retries?
            if (_blobs == null)
            {
                _blobs = GetAllBlobsAsync().Result;
            }

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
        fileName = GetFileName(fileName);

        string backupName = $"{fileName}_backup";
        if (fileName.Contains("pd"))
        {

            try
            {
                var res = await ReadBlobFile(fileName);
                if (res == null)
                {
                    await ForcePublish($"{fileName}_backup", originalJson, source: "PUBLISH");
                }
            }
            catch (Exception e)
            {
                await ForcePublish($"{fileName}_backup", originalJson, source: "PUBLISH");
                return true;
            }
        }
        return await ForcePublish(fileName, updatedJson, source: "PUBLISH");
    }

    private async Task<string> ForcePublishAndRead(string fileName, string json)
    {
        fileName = GetFileName(fileName);
        var deleted = await DeleteBlobFileIfExist(fileName);
        var created = await CreateAndUploadBlobFile(json, fileName);

        if (created)
        {
            return json;
        }
        else
        {
            LogError("Could Not UPLOAD FILE: " + fileName);
            Console.ReadLine();
            return null;
        }
    }

    private async Task<bool> ForcePublish(string fileName, string json, string source = "PUBLISH")
    {
        fileName = GetFileName(fileName);
        await DeleteBlobFileIfExist(fileName);
        return await CreateAndUploadBlobFile(json, fileName, source: source);
    }
}