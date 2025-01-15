using SharedLibrary.Models;
using Figgle;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Data;
using SharedLibrary.util;
using static SharedLibrary.util.Util;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
        private async Task<string> ProcessBlobAsync(IListBlobItem item, List<string> filedList, int installationId)
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

        private async Task<string> ProcessZipBlobAsync(CloudBlockBlob blobItem, int installationId)
        {
            var fileName = GetFileName(blobItem.Name);

            if (fileName.Contains("_BackUp")) { return ("BackupFile"); }

            bool isDayFile = fileName.Contains("pd");
            bool isMonthFile = fileName.Contains("pm");
            bool isYearFile = fileName.Contains("py");
            bool isTotalFile = fileName.Contains("pt");


            if (isDayFile)
            {
                return null;
                return await HandleDayFiles(blobItem, fileName, installationId);
            }
            else if (isMonthFile)
            {
                return await HandleMonthFiles(blobItem, fileName, installationId);

            }
            else
            {
                return String.Empty;
                throw new NotImplementedException();
            }

        }

        private async Task<string> HandleMonthFiles(CloudBlockBlob blobItem, string fileName, int installationId)
        {
            var originalJson = await ReadBlobFile(fileName + ".json", fileName + ".zip", installationId.ToString());
            var productionDto = ProductionDto.FromJson(originalJson);

            //Date handling
            var fileDate = fileName.Substring(2);
            int year = Convert.ToInt32(fileDate.Substring(0, 4));
            int month = Convert.ToInt32(fileDate.Substring(fileDate.Length - 2));
            var dataDate = new DateTime(year, month, 1);

            //Files name handling
            var daysFileNames = new List<string>();
            int daysInMonth = DateTime.DaysInMonth(year, month);
            for (int i = 1; i <= daysInMonth; i++)
            {
                daysFileNames.Add($"pd{fileDate}{i.ToString("D2")}");
            }

            //Fetch files
            var daysData = new ConcurrentDictionary<string, ProductionDto>();
            var fetchedFiles = await Task.WhenAll(daysFileNames.Select(async day =>
            {
                var jsonResponse = await ReadBlobFile(day);
                var productionDto = ProductionDto.FromJson(jsonResponse);
                daysData.TryAdd(day, productionDto);
                return productionDto;
            }));

            var updatedAccMonth = new ProductionDto
            {
                Inverters = CloningHelper.DeepClone(fetchedFiles.FirstOrDefault())?.Inverters ?? new List<Inverter>()
            };

            Parallel.ForEach(updatedAccMonth.Inverters, inv =>
            {
                inv.Production = new List<DataPoint>();
            });

            // Data handling
            bool didChange = false;
            Parallel.ForEach(fetchedFiles, file =>
            {
                Parallel.ForEach(file.Inverters, inverter =>
                {
                    foreach (var production in inverter.Production)
                    {
                        var updatedDatapoint = new DataPoint()
                        {
                            TimeStamp = production.TimeStamp,
                            Quality = production.Quality,
                            Value = production.Value,
                        };
                        if (updatedDatapoint.Value >= ApplicationVariables.MaxEnergyInJoules)
                        {
                            updatedDatapoint.Value = 0;
                            didChange = true;
                        }
                        lock (updatedAccMonth)
                        {
                            updatedAccMonth?.Inverters?.FirstOrDefault(i => i.Id == inverter.Id)?
                            .Production.Add(updatedDatapoint);
                        }
                    }
                });

            });



            if (didChange)
            {
                fileName = GetFileName(blobItem.Name);

                var updatedJson = ProductionDto.ToJson(updatedAccMonth);
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

        private static async Task<string> HandleDayFiles(CloudBlockBlob blobItem, string fileName, int installationId)
        {
            var originalJson = await ReadBlobFile(fileName + ".json", fileName + ".zip", installationId.ToString());

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
