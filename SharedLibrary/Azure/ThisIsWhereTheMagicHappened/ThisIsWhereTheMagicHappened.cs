﻿#pragma warning disable
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
using static SharedLibrary.Azure.AzureBlobCtrl;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
        public async Task<string> LetTheMagicHappen(DateOnly date)
        {
            var file = await ReadBlobFile(GetFileName(date, FileType.Day));

            if (!IsValidJson(file))
                return null;

            var result = await ReadAndErase(date);
            return result;
        }

        private async Task<string> ReadAndErase(DateOnly date)
        {
            //await HandleDayFiles(date);
            var res = await CleanYearDays(date);
            if (res)
                Log($"Cleaning done {date.Year}");
            else
                Log($"No cleaning needed {date.Year}");

            //await DeleteAllYearFilesExceptDays(date);
            //Log($"Deleted all yearFiles {date.Year}");

            await UpdatePDtoPM(date);
            Log($"PD -> PM DONE {date.Year}");

            await PMToYear(date);
            Log($"PM - > Year DONE {date.Year}");

            lock (ApplicationVariables.locktotalFile)
            {
                var result = YearToPT(date).Result;
            }
            Log($"Year -> PT DONE {date.Year}");

            return "success";
        }


        public async Task<bool> CleanYearDays(DateOnly date)
        {
            Log($"Remove All Junkies Day DataPoints pd{date.Year}", ConsoleColor.DarkBlue);

            var blobBlocks = await GetAllBlobsAsync();
            var blobs = blobBlocks.OfType<CloudBlockBlob>()
                                   .Where(blob => blob.Name.Contains($"pd{date.Year}"))
                                   .Where(blob => !blob.Name.Contains("BackUp")).ToList();

            var tasks = blobs.Select(async blob =>
            {
                try
                {
                    var fileName = GetFileName(blob.Name);
                    var originalJson = await ReadBlobFile(fileName);

                    var productionDto = ProductionDto.FromJson(originalJson);
                    bool didChange = false;

                    foreach (var inv in productionDto.Inverters)
                    {
                        foreach (var production in inv.Production)
                        {
                            if (production.Value >= ApplicationVariables.MaxEnergyInJoules)
                            {
                                Log($"{fileName}\tInverter: {inv.Id}\tValue: {production.Value} date {production.TimeStamp.Value.ToString()}");
                                production.Value = 0;
                                didChange = true;
                            }
                        }
                    }

                    if (didChange)
                    {
                        var updatedJson = ProductionDto.ToJson(productionDto);
                        //await BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);
                        await ForcePublish(fileName, updatedJson);
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

        async Task UpdatePDtoPM(DateOnly date)
        {
            //PD -> PM -> clouuudddd 🔥
            var yearDays = await GetYearDayFiles(date);
            var daysFile = yearDays.OrderBy(x => x.Date)
                                    .GroupBy(x => x.Date.Month)
                                    .ToList();

            var monthTasks = new List<Task>();
            foreach (var month in daysFile)
            {
                var monthGroup = month.ToList();

                monthTasks.Add(Task.Run(async () =>
                {
                    var result = await HandleMonthMagically_AndUploadThem(monthGroup);
                    return result;
                }));
            }

            await Task.WhenAll(monthTasks);
        }

        public async Task<string> PMToYear(DateOnly date)
        {
            try
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
                productions = productions.OrderBy(x => x.TimeStamp).ToList();

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

                await ForcePublish($"py{date.Year}", ProductionDto.ToJson(productionYear));
                var jsonResult = await ReadBlobFile($"py{date.Year}");

                return jsonResult;

            }
            catch (Exception e)
            {
                LogError($"YearFailed : {date.Year}" + e.Message);
            }
            return null;
        }

        async Task<string> YearToPT(DateOnly date)
        {
            try
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

                var res = string.Empty;

                res = await UploadProduction(productionTotal, FileType.Total);

                return res;
            }
            catch (Exception e)
            {
                LogError($"TotalFile failed :" + e.Message);
            }
            return null;
        }

        public async Task<string> UploadProduction(ProductionDto production, FileType fileType)
        {
            string fileName = string.Empty;

            string prodDay = $"{production.TimeStamp.Value.Day:D2}";
            string prodMonth = $"{production.TimeStamp.Value.Month:D2}";
            string prodYear = $"{production.TimeStamp.Value.Year}";

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
                Parallel.ForEach(month, day =>
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
                        }
                        );

                }
                );

                inverter.Production = inverter.Production.OrderBy(x => x.TimeStamp).ToList();
            });

            var production = new ProductionDto
            {
                TimeType = (int)FileType.Month,
                TimeStamp = inverters.First().Production.First().TimeStamp,
                Inverters = inverters.ToList()
            };

            var result = await UploadProduction(production, FileType.Month);

            if (IsValidJson(result))
                return result;
            else
                return null;
        }

        async Task<bool> DeleteAllYearFilesExceptDays(DateOnly date, FileType fileType = FileType.Day)
        {
            string fileName = $"{date.Year:D2}";

            try
            {
                var blocks = await GetAllBlobsAsync();
                var yearBlolbBlocks = blocks
                                      .Where(blob =>
                                          blob.Name.Contains(fileName)
                                          && !blob.Name.Contains("pd")
                                          //&& !blob.Name.Contains("BackUp")
                                          )
                                      .ToList();

                var tasks = new List<Task>();

                foreach (var blob in yearBlolbBlocks)
                {
                    tasks.Add(DeleteBlobFileIfExist(GetFileName(blob)));
                }

                await Task.WhenAll(tasks);
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

        // private object GetMonthFile(DateTime requestDate)
        // {
        //     for (int month = 1; month <= 12; month++)
        //     {
        //         for (int i = 0; i < requestDate.Year; i++)
        //         {
        //             var jsonResult = "null";
        //             return jsonResult;
        //         }
        //     }
        //
        //     return false;
        // }
    }


    public class HoodProduction
    {
        public FileType FileType { get; set; }

        public DateOnly Date { get; set; }

        public string DataJson { get; set; }
    }
}