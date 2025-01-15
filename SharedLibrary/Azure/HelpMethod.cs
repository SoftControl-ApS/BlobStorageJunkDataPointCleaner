using SharedLibrary.Models;
using Figgle;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Data;
using SharedLibrary.util;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
        private static async Task<string> ProcessBlobAsync(IListBlobItem item, List<string> filedList, int installationId)
        {
            if (item is not CloudBlockBlob blobItem)
                return String.Empty;

            filedList.Add(blobItem.Name);

            if (!await blobItem.ExistsAsync())
            {
                Log($"Filen '{blobItem.Name}' eksisterer ikke.", ConsoleColor.Red);
                return String.Empty;
            }

            return await ProcessZipBlobAsync(blobItem, installationId);
        }

        private static async Task<string> ProcessZipBlobAsync(CloudBlockBlob blobItem, int installationId)
        {
            var fileName = GetFileName(blobItem.Name);

            if (fileName.Contains("_BackUp")) { throw new DataException("BackupFile"); }

            bool isDayFile = fileName.Contains("pd");
            bool isMonthFile = fileName.Contains("pm");
            bool isYearFile = fileName.Contains("py");
            bool isTotalFile = fileName.Contains("pt");


            if (isDayFile)
            {
                return await HandleDayFiles(blobItem, fileName, installationId);
            }
            else if (isMonthFile)
            {

            }

            throw new NotImplementedException();
        }

        private static async Task<string> HandleDayFiles(CloudBlockBlob blobItem, string fileName, int installationId)
        {
            var originalJson = await ReadBlobFileJson(fileName + ".json", fileName + ".zip", installationId.ToString());

            var productionDto = ProductionDto.FromJson(originalJson);

            ExportToCSV(productionDto, blobItem.Name);

            bool didChange = false;

            Parallel.ForEach(productionDto.Inverters, inv =>
            {
                foreach (var production in inv.Production)
                {
                    if (production.Value >= ApplicationVariables.MaxEnergyInJoules)
                    {
                        production.Value = 0;
                        didChange = true;
                    }
                }
            });

            if (didChange)
            {
                fileName = GetFileName(blobItem.Name);

                var updatedJson = ProductionDto.ToJson(productionDto);
                await WriteJson(json: originalJson, fileName: $"{fileName}_BackUp", sn: installationId.ToString());// Backup
                await DeleteBlobFile(fileName, installationId); // Delete original
                await WriteJson(json: updatedJson, fileName: fileName, sn: installationId.ToString());    // Uploadedre
                return originalJson;
            }
            else
            {
                return originalJson;
            }
        }
    }
}
