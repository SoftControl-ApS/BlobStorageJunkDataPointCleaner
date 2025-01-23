#pragma warning disable
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SharedLibrary.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static SharedLibrary.util.Util;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
        public async Task<bool> LetTheMagicHappen(DateOnly date)
        {
            var result = await ReadAndErase(date);

            return false;
        }

        private async Task<string> ReadAndErase(DateOnly date)
        {
            await HandleDayFiles(date);
            var res = await CleanYearDays(date);
            if (res)
                Title("Cleaning done");
            else
                Title("No cleaning needed");

            await DeleteAllYearFilesExceptDays(date);
            Title("Deleted all yearFiles");

            await PDToPm(date);
            Title("PD -> PM DONE");

            await PMToYear(date);
            Title("PM - > Year DONE");

            await YearToPT(date);
            Title("Year -> PT DONE");



            return "success";
        }


        public async Task<bool> CleanYearDays(DateOnly date)
        {
            Title("Remove All Junkies Day DataPoints", ConsoleColor.Cyan);

            var blobs = _blobBLocks.OfType<CloudBlockBlob>()
                                   .Where(blob => blob.Name.Contains($"pd{date.Year}"))
                                   .Where(blob => !blob.Name.Contains("BackUp")).ToList();

            var tasks = blobs.Select(async blob =>
            {
                try
                {
                    var fileName = GetFileName(blob.Name);
                    var originalJson = await ReadBlobFile(fileName + ".json", fileName + ".zip", InstallationId);

                    var productionDto = ProductionDto.FromJson(originalJson);
                    bool didChange = false;

                    foreach (var inv in productionDto.Inverters)
                    {
                        foreach (var production in inv.Production)
                        {
                            if (production.Value >= ApplicationVariables.MaxEnergyInJoules)
                            {
                                Log($"{fileName}\tInverter: {inv.Id}\tValue: {production.Value}");
                                production.Value = 0;
                                didChange = true;
                            }
                        }
                    }

                    if (didChange)
                    {
                        var updatedJson = ProductionDto.ToJson(productionDto);
                        await BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error processing blob {blob.Name}: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);

            return true;
        }

        async Task PDToPm(DateOnly date)
        {
            //PD -> PM -> clouuudddd 🔥
            var daysFiles = await GetYear_DayFilesAsync(date);
            var filteredDayFiles = daysFiles.OrderBy(x => x.Date)
                                            .GroupBy(x => x.Date.Month)
                                            .ToList();

            var monthTasks = new List<Task>();
            foreach (var month in filteredDayFiles)
            {
                monthTasks.Add(HandleMonthMagically_AndUploadThem(month.ToList()));
            }

            await Task.WhenAll(monthTasks);
        }

        public async Task<string> PMToYear(DateOnly date)
        {
            // PM -> PY 🧸
            var yearMonthsFiles = await GetYear_MonthFilessAsync(date);

            var inverters = InitializeInverters(ProductionDto.FromJson(await ReadBlobFile("pt")).Inverters);

            var productions = new List<ProductionDto>();
            Parallel.ForEach(yearMonthsFiles, month =>
            {
                var prod = ProductionDto.FromJson(month.DataJson);
                productions.Add(prod);
            });
            productions.OrderBy(x => x.TimeStamp);

            foreach (var inverter in inverters)
            {
                foreach (var production in productions)
                {
                    double totalProduction = 0;
                    var inv = production.Inverters.First(x => x.Id == inverter.Id);
                    double sum = (double)inv.Production.Sum(x => x.Value);
                    totalProduction += sum;

                    inverter.Production.Add(new DataPoint()
                    {
                        Quality = 1,
                        TimeStamp = new DateTime(production.TimeStamp.Value.Year, production.TimeStamp.Value.Month, 1),
                        Value = totalProduction
                    });
                }

                inverter.Production = inverter.Production.OrderBy(x => x.TimeStamp).ToList();
            }

            var productionYear = new ProductionDto()
            {
                Inverters = inverters.ToList(),
                TimeType = (int)FileType.Year,
                TimeStamp = new DateTime(date.Year, 1, 1)
            };

            await UploadProduction(productionYear, FileType.Year);
            var jsonResult = await ReadBlobFile($"py{date.Year:D4}");

            return jsonResult;
        }

        async Task<string> YearToPT(DateOnly? date = null)
        {
            //PY -> PT -> clouuudddd 🔥
            var productions = await GetYearsAsync();
            var inverters = InitializeInverters((ProductionDto.FromJson(await ReadBlobFile("pt"))).Inverters);

            Parallel.ForEach(inverters, inverter =>
            {
                foreach (var production in productions)
                {
                    double totalProduction = 0;
                    var inv = production.Inverters.First(x => x.Id == inverter.Id);
                    double sum = (double)inv.Production.Sum(x => x.Value);
                    totalProduction += sum;

                    inverter.Production.Add(new DataPoint()
                    {
                        Quality = 1,
                        TimeStamp = new DateTime(production.TimeStamp.Value.Year, 12, 31),
                        Value = totalProduction
                    });
                }

                inverter.Production = inverter.Production.OrderBy(x => x.TimeStamp).ToList();
            });


            var productionTotal = new ProductionDto()
            {
                Inverters = inverters.ToList(),
                TimeType = (int)FileType.Total,
                TimeStamp = new DateTime(2014, 1, 1)
            };


            await UploadProduction(productionTotal, FileType.Total);
            var jsonResult = await ReadBlobFile($"pt");

            return jsonResult;
        }

        public async Task<bool> CreatAndUploadProduction(List<Inverter> inverters, FileType fileType)
        {
            var date = inverters.First().Production.First().TimeStamp.Value;
            var productionMont = new ProductionDto()
            {
                Inverters = inverters.ToList(),
                TimeType = (int)fileType
            };
            string fileName = string.Empty;

            switch (fileType)
            {
                case FileType.Day:
                    productionMont.TimeStamp = new DateTime(date.Year, date.Minute, date.Day);
                    fileName = $"pd{date.Year:D4}{date.Month:D2}{date.Day:D2}";
                    break;
                case FileType.Month:
                    productionMont.TimeStamp = new DateTime(date.Year, date.Month, 1);
                    fileName = $"pm{date.Year:D4}{date.Month:D2}";
                    break;
                case FileType.Year:
                    productionMont.TimeStamp = new DateTime(date.Year, 1, 1);
                    fileName = $"py{date.Year:D4}";
                    break;
                case FileType.Total:
                    productionMont.TimeStamp = new DateTime(2000, 1, 1);
                    fileName = $"pt";
                    break;
            }

            var productionJson = ProductionDto.ToJson(productionMont);

            return await CreateAndUploadBlobFile(productionJson, fileName);
        }

        public async Task<string> UploadProduction(ProductionDto production, FileType fileType)
        {
            string fileName = string.Empty;

            string prodDay = $"{production.TimeStamp.Value.Day:D2}";
            string prodMonth = $"{production.TimeStamp.Value.Month:D2}";
            string prodYear = $"{production.TimeStamp.Value.Year:D4}";

            switch (fileType)
            {
                case FileType.Day:
                    fileName = $"pd{prodYear}{prodMonth}{prodDay}";
                    break;
                case FileType.Month:
                    fileName = $"pm{prodYear}{prodMonth}";
                    break;
                case FileType.Year:
                    fileName = $"py{prodYear}";
                    break;
                case FileType.Total:
                    fileName = $"pt";
                    break;
            }

            var productionJson = ProductionDto.ToJson(production);
            return await ForcePublish(fileName, productionJson);
        }

        private async Task<string> HandleMonthMagically_AndUploadThem(List<HoodProduction> month)
        {
            var inverters = InitializeInverters(ProductionDto.FromJson(month.FirstOrDefault().DataJson).Inverters);
            Parallel.ForEach(inverters, inverter =>
            {
                foreach (var day in month)
                {
                    double totalMonthProduction = 0;
                    ProductionDto productionDay = ProductionDto.FromJson(day.DataJson);
                    totalMonthProduction +=
                        (double)
                        productionDay.Inverters.First(x => x.Id == inverter.Id)
                                     .Production.Sum(x => x.Value);

                    inverter.Production.Add(
                        new DataPoint()
                        {
                            Quality = 1,
                            TimeStamp = new DateTime(productionDay.TimeStamp.Value.Year,
                                productionDay.TimeStamp.Value.Month, productionDay.TimeStamp.Value.Day),
                            Value = totalMonthProduction
                        });
                }
            });


            await CreatAndUploadProduction(inverters.ToList(), FileType.Month);
            return await ReadBlobFile(
                $"pm{
                    inverters.First()
                    .Production.First()
                    .TimeStamp.Value.Year}{
                    inverters.First()
                    .Production.First()
                    .TimeStamp.Value.Month:D2}");
        }

        async Task<bool> DeleteAllYearFilesExceptDays(DateOnly date, FileType fileType = FileType.Day)
        {
            string fileName = $"{date.Year:D2}";

            try
            {
                var yearBlolbBlocks = _blobBLocks
                                      .Where(blob =>
                                          blob.Name.Contains(fileName) && !blob.Name.Contains("pd") &&
                                          !blob.Name.Contains("BackUp"))
                                      .ToList();

                var tasks = new List<Task>();

                foreach (var blob in yearBlolbBlocks)
                {
                    tasks.Add(DeleteBlobFileIfExist(GetFileName(blob)));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
                return true;
            }
            catch (Exception e)
            {
                LogError("Could not delete all year files, year:" + date.Year);
                LogError(e.Message);
                return false;
                throw;
            }
        }

        private object GetMonthFile(DateTime requestDate)
        {
            for (int month = 1; month <= 12; month++)
            {
                for (int i = 0; i < requestDate.Year; i++)
                {
                    var jsonResult = "null";
                    return jsonResult;
                }
            }

            return false;
        }
    }


    public class HoodProduction
    {
        public FileType FileType { get; set; }

        public DateOnly Date { get; set; }

        public string DataJson { get; set; }
    }
}