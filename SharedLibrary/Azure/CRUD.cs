using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
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

        public static async Task DeleteBlobFile(string zip, object installationId, string containerName = "installations")
        {
            CloudBlockBlob blobFile = GetBlockBlobReference(zip, installationId.ToString(), containerName);
            var result = await blobFile.DeleteIfExistsAsync();
            Console.WriteLine($"Blob '{zip}' in installation '{installationId}' deleted successfully.");
        }

        public async Task<string> ReadBlobFile(string fileName)
        {
            string json = "null";

            try
            {
                json = await ReadBlobFile(fileName + ".json", fileName + ".zip", InstallationId.ToString());

            }
            catch (Exception ex)
            {
                return json;
            }

            return json;
        }
        public static async Task<string> ReadBlobFile(string fileName, string zipFileName, string sn, bool acquireLock = true)
        {
            if (!zipFileName.EndsWith(".zip"))
                throw new ArgumentException("file name must end with .zip", nameof(zipFileName));
            if (!fileName.EndsWith(".json"))
                throw new ArgumentException("file name must end with .zip", nameof(fileName));

            if (acquireLock)
            {
                var installationLock = GetLock(fileName, zipFileName, sn);
                installationLock.AcquireWriterLock(LockTimeOut);
            }
            string json = "null";

            CloudBlockBlob blobFile = GetBlockBlobReference(zipFileName, sn);

            using (var zipStream = new MemoryStream())
            {
                await blobFile.DownloadToStreamAsync(zipStream);

                ZipArchive archive = new ZipArchive(zipStream);

                ZipArchiveEntry zeipArchiveEntry = archive.GetEntry(fileName);

                using (StreamReader sr = new StreamReader(zeipArchiveEntry.Open()))
                {
                    json = sr.ReadToEnd();
                }
            }

            return json;
        }

        public static async Task<bool> WriteJson(string json, string fileName, string sn)
        {

            fileName = GetFileName(fileName);

            try
            {
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
            catch (Exception ex)
            {
                Log("[SunSyst<emJsonFile] WriteJson() Exception:" + ex.ToString());
            }

            return false;
        }
    }
}
