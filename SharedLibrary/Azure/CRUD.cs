﻿using Microsoft.WindowsAzure.Storage;
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
using SharedLibrary.SunSys;
using System.Numerics;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
        #region Create
        public async Task<bool> CreateAndUploadBlobFile(string jsonContent, string fileName, string containerName = "installations")
        {
            string zipFileName = $"{fileName}.zip";
            CloudBlobContainer container = GetContainerReference(containerName);
            CloudBlockBlob blobFile = container.GetBlockBlobReference($"{InstallationId}/{zipFileName}");

            using (MemoryStream compressedStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(compressedStream, ZipArchiveMode.Create, true))
                {
                    var jsonFileEntry = archive.CreateEntry($"{fileName}.json");

                    using (var entryStream = jsonFileEntry.Open())
                    using (var streamWriter = new StreamWriter(entryStream))
                    {
                        await streamWriter.WriteAsync(jsonContent);
                    }
                }

                compressedStream.Seek(0, SeekOrigin.Begin);
                await blobFile.UploadFromStreamAsync(compressedStream);
            }
            Log($"{fileName} production object was created", ConsoleColor.Yellow);

            await initBlobBlocks();
            return true;
        }
        #endregion
        #region Read
        public async Task<string> ReadBlobFile(string fileName, bool acquireLock = true)
        {
            return await ReadBlobFile(fileName + ".json", fileName + ".zip", InstallationId);
        }
        
        public async Task<string> ReadBlobFile(string fileName, string sn = null, bool createIfNotFoud = false)
        {
            if (string.IsNullOrEmpty(sn) && !string.IsNullOrEmpty(InstallationId))
            {
                sn = InstallationId;
            }
            else if (string.IsNullOrEmpty(sn) && string.IsNullOrEmpty(InstallationId))
            {
                throw new ArgumentException("Either 'InstallationId' or'sn' parameter must be provided.");
            }

            string json;
            if (createIfNotFoud)
            {
                json = ReadBlobFileAndCreateIfNotFound(fileName + ".json", fileName + ".zip", sn).Result;
                Log($"Created and Read file {fileName} successfully", ConsoleColor.Yellow);
            }
            else
            {
                json = ReadBlobFile(fileName + ".json", fileName + ".zip", sn).Result;
                Log($"Read file {fileName} successfully", ConsoleColor.DarkYellow);
            }
            await initBlobBlocks();
            return json;
        }

        public async Task<string> ReadBlobFile(string fileName, string zipFileName, string sn = null, bool acquireLock = true)
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

            CloudBlockBlob blobFile = await GetBlockBlobReference(zipFileName, sn);

            using (var zipStream = new MemoryStream())
            {
                try
                {
                    var a = await blobFile.ExistsAsync();
                    if (a == false)
                        throw new NullReferenceException(nameof(a));
                }
                catch (NullReferenceException e)
                {
                    LogError(e.Message);
                    return "NOTFOUND";
                }

                await blobFile.DownloadToStreamAsync(zipStream);
                ZipArchive archive = new ZipArchive(zipStream);
                ZipArchiveEntry zipArchiveEntry = archive.GetEntry(fileName);
                using (StreamReader sr = new StreamReader(zipArchiveEntry.Open()))
                {
                    json = await sr.ReadToEndAsync();
                }
            }

            return json;
        }

        public async Task<string> ReadBlobFileAndCreateIfNotFound(string fileName, string zipFileName, string sn = null, bool acquireLock = true)
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

            CloudBlockBlob blobFile = await GetBlockBlobReference(zipFileName, sn);

            using (var zipStream = new MemoryStream())
            {
                try
                {
                    await blobFile.ExistsAsync();
                }
                catch (NullReferenceException e)
                {
                    LogError(e.Message);

                    if (fileName.Contains("py"))
                    {
                        return await GenerateAndUploadEmptyYearFile(fileName);
                    }
                    if (fileName.Contains("dp"))
                    {
                        return await GenerateAndUploadEmptyDayFile(fileName);
                    }

                    return "NOTFOUND";
                }

                await blobFile.DownloadToStreamAsync(zipStream);
                ZipArchive archive = new ZipArchive(zipStream);
                ZipArchiveEntry zipArchiveEntry = archive.GetEntry(fileName);
                using (StreamReader sr = new StreamReader(zipArchiveEntry.Open()))
                {
                    json = await sr.ReadToEndAsync();
                }
            }

            return json;
        }

        #endregion
        #region update
        public async Task<bool> WriteJson(string json, string fileName, string? sn = null)
        {
            sn = ValidateFileName(fileName, sn);

            CloudBlockBlob blobFile = await GetBlockBlobReference($"{fileName}.zip", sn);

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
            await initBlobBlocks();
            return true;
        }
        #endregion
        #region Delete
        public async Task DeleteBlobFile(string zip, string installationId,
                                         string containerName = "installations")
        {
            if (!zip.EndsWith(".zip"))
            {
                zip = zip + ".zip";
            }

            CloudBlockBlob blobFile = await GetBlockBlobReference(zip, containerName);
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

            await initBlobBlocks();
        }
        public async Task DeleteBlobFileIfExist(string zip, string containerName = "installations")
        {
            try
            {
                CloudBlobContainer container = GetContainerReference(containerName);
                var blobFile = container.GetBlockBlobReference($"{InstallationId}/{zip}");
                await blobFile.DeleteIfExistsAsync();

            }
            catch (Exception ex)
            {

            }
            if (!zip.EndsWith(".zip"))
            {
                zip = zip + ".zip";
            }
            await initBlobBlocks();
        }
        public async Task DeleteBlobFile(string zip,
                                         string containerName = "installations")
        {
            if (string.IsNullOrEmpty(InstallationId))
                throw new ArgumentException("InstallationId' parameter must be provided.");

            await DeleteBlobFile(zip, InstallationId, containerName);
        }
        #endregion
    }
}