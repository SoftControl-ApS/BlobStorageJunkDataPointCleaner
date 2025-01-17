using SharedLibrary.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using static SharedLibrary.util.Util;
using System.Collections.Concurrent;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
        private async Task<string> ProcessBlobAsync(IListBlobItem item)
        {
            if (item is not CloudBlockBlob blobItem)
                return String.Empty;

            if (!await blobItem.ExistsAsync())
            {
                Log($"Filen '{blobItem.Name}' eksisterer ikke.", ConsoleColor.Red);
                return String.Empty;
            }

            var fileName = GetFileName(blobItem.Name);
            if (fileName.Contains("_BackUp"))
            {
                return ("BackupFile");
            }
            FileType fileType = FileType.Day;
            
            if (fileName.Contains("pd"))
            {
                fileType = FileType.Day;
            }
            // else if (fileName.Contains("pm"))
            // {
            //     fileType = FileType.Month;
            // }
            // else if (fileName.Contains("py"))
            // {
            //     fileType = FileType.Year;
            // }
            // else if (fileName.Contains("pt"))
            // {
            //     fileType = FileType.Total;
            // }
            else
            {
                LogError("Unsupported FileType");
                return null;    
            }

            return await ProcessZipBlobAsync(blobItem, fileType);
        }


        private async Task<string> ProcessZipBlobAsync(CloudBlockBlob blobItem, FileType fileType)
        {
            string fileName = GetFileName(blobItem.Name);
            switch (fileType)
            {
                case FileType.Day:
                    return await HandleDayFiles(fileName);
                default:
                    return null;
            }
        }

        private async Task<string> FixFile(FileType fileType, DateOnly date)
        {
            switch (fileType)
            {
                case FileType.Day:
                    return await HandleDayFiles(date);
                case FileType.Month:
                    return await UpdateMonthFiles(date);
                case FileType.Year:
                    return await UpdateYearFiles(date);
                case FileType.Total:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
            }
        }

        private List<Inverter> InitializeInverters(IEnumerable<Inverter> inverters)
        {
            var updatedInverters = new ConcurrentBag<Inverter>();
            Parallel.ForEach(inverters, inverter =>
            {
                updatedInverters.Add(new Inverter
                                     {
                                         Id = inverter.Id,
                                         Production = new List<DataPoint>(),
                                     });
            });
            return updatedInverters.ToList();
        }

        private async Task<List<Inverter>> UpdateInverterProductionData(IEnumerable<Inverter> inverters, int year)
        {
            var updatedInverters = new List<Inverter>();
            var tasks = inverters.SelectMany(inverter => Enumerable.Range(1, 12).Select(month => Task.Run(async () =>
            {
                var dateTime = new DateTime(year, month, 1);
                var date = DateOnly.FromDateTime(dateTime);
                double totalProduction = await GetInverterTotalMonthProduction(date, (int)inverter.Id);
                updatedInverters.First(inv => inv.Id == inverter.Id).Production.Add(new DataPoint
                    {
                        TimeStamp = dateTime,
                        Quality = 1,
                        Value = totalProduction
                    });
            })));

            await Task.WhenAll(tasks);
            return updatedInverters;
        }

        private async Task<List<Inverter>> UpdateInverterProductionData(
            IEnumerable<Inverter> inverters, DateOnly date)
        {
            var updatedInverters = new ConcurrentBag<Inverter>();
            var tasks = new List<Task>();
            var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);

            foreach (var inverter in inverters)
            {
                updatedInverters.Add(new Inverter
                                     {
                                         Id = inverter.Id,
                                         Production = new List<DataPoint>(),
                                     });

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var datapointTimeStamp = new DateTime((int)date.Year, (int)date.Month, day);
                    var reqDate = DateOnly.FromDateTime(datapointTimeStamp);
                    double totalProduction = await GetInverterTotalMonthProduction(reqDate, (int)inverter.Id);

                    updatedInverters.First(inv => inv.Id == inverter.Id)
                                    .Production.Add(new DataPoint
                                                    {
                                                        TimeStamp = datapointTimeStamp,
                                                        Quality = 1,
                                                        Value = totalProduction
                                                    });
                }
            }

            return updatedInverters.ToList();
        }

        private ProductionDto CreateUpdatedProductionDto(ProductionDto oldProduction,
                                                         IList<Inverter> inverters)
        {
            return new ProductionDto
                   {
                       Inverters = inverters.ToList(),
                       TimeType = oldProduction.TimeType,
                       TimeStamp = oldProduction.TimeStamp,
                   };
        }

        private int ExtractYearFromFileName(string fileName)
        {
            if (fileName.Length < 6)
            {
                throw new ArgumentException("fileName must be at least 6 characters");
            }

            return Convert.ToInt32(fileName.Substring(2, 4));
        }
    }
}