using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using static SharedLibrary.ApplicationVariables;
using static Microsoft.WindowsAzure.Storage.CloudStorageAccount;
using SharedLibrary.util;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
        public static async Task DeleteBlobFile(string zip, string installationId,
                                                string containerName = "installations")
        {
            if (!zip.EndsWith(".zip"))
            {
                zip = zip + ".zip";
            }

            CloudBlockBlob blobFile = GetBlockBlobReference(zip, installationId.ToString(), containerName);

            var exists = await blobFile.ExistsAsync();
            if (!exists)
            {
                LogError($"Blob '{zip}' in installation '{installationId}' does not exist.");
            }
            else
            {
                var result = await blobFile.DeleteIfExistsAsync();
                Log($"Blob '{zip}' in installation '{installationId}' deleted {result}.");
            }
        }

        public async Task DeleteBlobFile(string zip,
                                         string containerName = "installations")
        {
            if (string.IsNullOrEmpty(InstallationId))
                throw new ArgumentException("InstallationId' parameter must be provided.");

            DeleteBlobFile(zip, InstallationId, containerName);
        }

        public async Task<string> ReadBlobFile(string fileName, string sn = null)
        {
            if (string.IsNullOrEmpty(sn) && !string.IsNullOrEmpty(InstallationId))
            {
                sn = InstallationId;
            }
            else if (string.IsNullOrEmpty(sn) && string.IsNullOrEmpty(InstallationId))
            {
                throw new ArgumentException("Either 'InstallationId' or'sn' parameter must be provided.");
            }

            string json = "null";
            json = ReadBlobFile(fileName + ".json", fileName + ".zip", sn).Result;

            return json;
        }

        public async Task<string> ReadBlobFile(string fileName, string zipFileName, string sn = null,
                                               bool acquireLock = true)
        {
            if (string.IsNullOrEmpty(sn) && !string.IsNullOrEmpty(InstallationId))
                sn = InstallationId;
            else if (string.IsNullOrEmpty(sn) && string.IsNullOrEmpty(InstallationId))
                throw new ArgumentException("Either 'InstallationId' or'sn' parameter must be provided.");

            if (!zipFileName.EndsWith(".zip"))
                throw new ArgumentException("file name must end with .zip", nameof(zipFileName));
            if (!fileName.EndsWith(".json"))
                throw new ArgumentException("file name must end with .zip", nameof(fileName));

            string json = "null";

            CloudBlockBlob blobFile = GetBlockBlobReference(zipFileName, sn);

            using (var zipStream = new MemoryStream())
            {
                var exists = await blobFile.ExistsAsync();
                if (!exists)
                {
                    LogError($"Blob '{fileName}' in installation '{InstallationId}' does not exist.");
                    return null;
                }

                await blobFile.DownloadToStreamAsync(zipStream);

                ZipArchive archive = new ZipArchive(zipStream);

                ZipArchiveEntry zeipArchiveEntry = archive.GetEntry(fileName);

                using (StreamReader sr = new StreamReader(zeipArchiveEntry.Open()))
                {
                    json = await sr.ReadToEndAsync();
                }
            }

            return json;
        }

        public async Task<bool> WriteJson(string json, string fileName, string? sn = null)
        {
            fileName = GetFileName(fileName);
            if (string.IsNullOrEmpty(sn) && !string.IsNullOrEmpty(InstallationId))
            {
                sn = InstallationId;
            }
            else if (string.IsNullOrEmpty(sn) && string.IsNullOrEmpty(InstallationId))
            {
                throw new ArgumentException("Either 'InstallationId' or'sn' parameter must be provided.");
            }

            CloudBlockBlob blobFile = GetBlockBlobReference($"{fileName}.zip", sn);

            using (MemoryStream ms = new MemoryStream(UTF8Encoding.UTF8.GetBytes(json)))
            {
                using (MemoryStream compressed = new MemoryStream())
                {
                    using (var archive = new ZipArchive(compressed, ZipArchiveMode.Create, true))
                    {
                        var jsonFile = archive.CreateEntry($"{fileName}.json");

                        using (var entryStream = jsonFile.Open())
                        using (var streamWriter = new StreamWriter(entryStream))
                        {
                            await streamWriter.WriteAsync(json);
                        }
                    }

                    compressed.Seek(0, SeekOrigin.Begin);
                    compressed.Position = 0;
                    await blobFile.UploadFromStreamAsync(compressed).ConfigureAwait(false);
                }
            }

            return true;
        }
    }
}