using SharedLibrary.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using static SharedLibrary.util.Util;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {

        private ConcurrentBag<Inverter> _inverters = new ConcurrentBag<Inverter>();
        private ConcurrentBag<Inverter> Inverters
        {
            get => _inverters;
            set
            {
                _inverters = value;
            }
        }

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

        //private async Task<string> FixFile(FileType fileType, DateOnly date)
        //{
        //    switch (fileType)
        //    {
        //        case FileType.Day:
        //            return await HandleDayFiles(date);
        //        case FileType.Month:
        //            return await UpdateMonthFiles(date);
        //        case FileType.Year:
        //            return await UpdateYearFiles(date);
        //        case FileType.Total:
        //            return await UpdateTotalFile(date);
        //        default:
        //            throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
        //    }
        //}


        private async Task <ConcurrentBag<Inverter>> ExtractInverters(ProductionDto production)
        {
            if (production == null)
            {
                var inverters = GetInverters().Result;
                return inverters;
            }

            return ExtractInverters(production.Inverters);
        }
        private List<Inverter> InitializeInvertersToList(IEnumerable<Inverter> inverters)
        {
            return ExtractInverters(inverters).ToList();
        }

        private ConcurrentBag<Inverter> ExtractInverters(IEnumerable<Inverter> inverters)
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
            return updatedInverters;
        }

        private async Task<ConcurrentBag<Inverter>> GetInverters()
        {
            if (!Inverters.Any())
            {
                return await InitInverters();
            }

            return Inverters;
        }

        async Task<ConcurrentBag<Inverter>> InitInverters()
        {
            var production = ProductionDto.FromJson(await ReadBlobFile("pt"));
            var invs = await ExtractInverters(production);
            this._inverters = invs;
            return _inverters;
        }

        

        //private async Task<List<Inverter>> UpdateInverterProductionData(IEnumerable<Inverter> inverters, int year)
        //{
        //    var updatedInverters = InitializeInvertersToList(inverters);
        //    var tasks = inverters.SelectMany(inverter => Enumerable.Range(1, 12).Select(month => Task.Run(async () =>
        //    {
        //        var dateTime = new DateTime(year, month, 1);
        //        var date = DateOnly.FromDateTime(dateTime);

        //        double? totalProduction = await GetInverterTotalMonthProduction(date, (int)inverter.Id);
        //        if (totalProduction == null)
        //        {
        //            updatedInverters.First(inv => inv.Id == inverter.Id).Production.Add(new DataPoint
        //                {
        //                    TimeStamp = dateTime,
        //                    Quality = 1,
        //                    Value = 0
        //                });
        //        }
        //        else
        //        {
        //            updatedInverters.First(inv => inv.Id == inverter.Id).Production.Add(new DataPoint
        //                {
        //                    TimeStamp = dateTime,
        //                    Quality = 1,
        //                    Value = totalProduction
        //                });
        //        }
        //    })));

        //    await Task.WhenAll(tasks);
        //    return OrderInverterDataPointsByDate(updatedInverters);
        //}

        //private async Task<List<Inverter>> UpdateInverterProductionData(
        //    IEnumerable<Inverter> inverters, DateOnly date)
        //{
        //    var updatedInverters = new ConcurrentBag<Inverter>();
        //    var tasks = new List<Task>();
        //    var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);

        //    foreach (var inverter in inverters)
        //    {
        //        updatedInverters.Add(new Inverter
        //                             {
        //                                 Id = inverter.Id,
        //                                 Production = new List<DataPoint>(),
        //                             });

        //        for (int day = 1; day <= daysInMonth; day++)
        //        {
        //            var datapointTimeStamp = new DateTime((int)date.Year, (int)date.Month, day);
        //            var reqDate = DateOnly.FromDateTime(datapointTimeStamp);
        //            double totalProduction = (double)await GetInverterTotalMonthProduction(reqDate, (int)inverter.Id);

        //            updatedInverters.First(inv => inv.Id == inverter.Id)
        //                            .Production.Add(new DataPoint
        //                                            {
        //                                                TimeStamp = datapointTimeStamp,
        //                                                Quality = 1,
        //                                                Value = totalProduction
        //                                            });
        //        }
        //    }

        //    return updatedInverters.ToList();
        //}

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

        private int ExtractMonthFromFileName(string fileName)
        {
            if (fileName.Length < 8)
            {
                throw new ArgumentException("fileName must be at least 8 characters");
            }

            var result = (fileName.Substring(6, 2));
            var formated = $"{result:D2}";
            return Convert.ToInt32(formated);
        }

        private int ExtractDayFromFileName(string fileName)
        {
            if (fileName.Length < 10)
            {
                throw new ArgumentException("fileName must be at least 10 characters");
            }

            return Convert.ToInt32(fileName.Substring(8, 2));
        }

        private DateOnly ExtractDateFromFileName(string fileName)
        {
            int year = ExtractYearFromFileName(fileName);
            int month = ExtractMonthFromFileName(fileName);
            int day = 01;
            if (fileName.Length > 8)
                day = ExtractDayFromFileName(fileName);

            DateOnly date = new DateOnly(year, month, day);
            return date;
        }


        public List<Inverter> OrderInverterDataPointsByDate(List<Inverter> inverters)
        {
            Parallel.ForEach(inverters, inverter =>
            {
                inverter.Production = inverter.Production
                                              .OrderBy(dp => dp.TimeStamp)
                                              .ToList();
            });

            return inverters;
        }

        public string ValidateFileName(string fileName, string? sn)
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

            return sn;
        }
    }
}