#pragma warning disable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SharedLibrary.Azure;
using SharedLibrary.Models;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static SharedLibrary.Azure.AzureBlobCtrl;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    private ConcurrentDictionary<string, string> _editedFiles = new ConcurrentDictionary<string, string>();
    private string _ptJson = string.Empty;
    private ProductionDto _ptProductionDto;
    public object ptFile { get; set; } = new object();

    public async Task LoadPT()
    {
        _ptJson = await ReadBlobFile("pt");

        if (IsValidJson(_ptJson)) // PR: Can be removed
        {
            _ptProductionDto = ProductionDto.FromJson(_ptJson);
        }
    }

    public async Task<bool> CheckForExistingFiles(DateTime date)
    {
        return await CheckForExistingFiles(DateOnly.FromDateTime(date));
    }
    public async Task<bool> CheckForExistingFiles(DateOnly date)
    {
        try
        {
            var blobs = (await GetAllBlobsAsync())
                         .Where(blob => !blob.Name.Contains("BackUp"))
                         .Where(blob => blob.Name.Contains($"pd{date.Year}") || blob.Name.Contains($"pm{date.Year}") || blob.Name.Contains($"py{date.Year}")).ToList();

            return blobs.Any();
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public async Task<string> LetTheMagicHappen(DateOnly date)
    {

        var res = await CleanYear_AllDaysFiles(date);
        if (res)
        {
            Log($"Cleaning done {date.Year}");
        }
        else
        {
            Log($"No cleaning needed {date.Year}");
        }
        if (!_editedFiles.Any())
        {
            Log("No Updated needed");
        }
        else
        {
            Log($"Removed All junks Day DataPoints {date.ToString()}", ConsoleColor.DarkBlue);
        }

        //await DeleteAllYearFilesExceptDays(date);
        //Log($"Deleted all yearFiles {date.Year}");

        await UpdatePDtoPM(date);
        Log($"PD -> PM DONE {date.Year}");

        await PMToYear(date);
        Log($"PM - > Year DONE {date.Year}");

        return "success";
    }

    public async Task<bool> CleanYear_AllDaysFiles(DateOnly date)
    {
        var blobs = _blobs
                    .OfType<CloudBlockBlob>() // PR: All blobs are CloudBlockBlob
                    .Where(blob => blob.Name.Contains($"pd{date.Year}"))
                    .Where(blob => !blob.Name.ToLower().Contains("backup"))
                    .ToList();

        var tasks = blobs.Select(async blob =>
        {
            var fileName = GetFileName(blob.Name);
            var originalJson = await ReadBlobFile(fileName);

            if (originalJson != null)
            {
                var productionDto = ProductionDto.FromJson(originalJson);
                bool didChange = false;


                foreach (var inv in productionDto.Inverters)
                {
                    foreach (var production in inv.Production)
                    {
                        if (production == null)
                        {
                            continue;
                        }

                        if (production.Value >= ApplicationVariables.MaxEnergyInJoules)
                        {
                            Log($"{fileName}\t" +
                                $"Inverter: {inv.Id}\t" +
                                $"Value: {production.Value} date {production.TimeStamp.Value.ToString()}"
                            );

                            production.Value = 0;
                            didChange = true;
                        }
                    }
                }

                if (didChange)
                {
                    var updatedJson = ProductionDto.ToJson(productionDto);
                    var result = await BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);
                    if (result)
                    {
                        _editedFiles.TryAdd(fileName, updatedJson); // PR: Remove to avoid memory leak
                        LogSuccess("fileName: " + fileName + " Was updated ");
                    }
                    else
                        LogError("Could Not Update filename: " + fileName);
                }
            }
            else
            {
                //GenerateAndUploadEmptyDayFile(fileName);
            }
        });

        await Task.WhenAll(tasks);
        return true;
    }

    public async Task UpdatePDtoPM(DateOnly date)
    {
        // PD -> PM -> clouuudddd 🔥
        try
        {
            var yearDays = await GetYearDayFiles(date);
            var monthsList = yearDays.OrderBy(x => x.Date).GroupBy(x => x.Date.Month).ToList();

            var sw = new Stopwatch();
            sw.Start();


            foreach (var month in monthsList)
            {
                var monthGroup = month.ToList();
                var result = await ProcessInverterProductionAsync(monthGroup);
                if (!IsValidJson(result)) // PR: Can be removed
                {
                    LogError("Could Not Update PD");
                }
            }

            sw.Stop();
            LogSuccess($"Retrieving all days files took {sw.ElapsedMilliseconds / 1000} Seconds");
        }
        catch (Exception e)
        {
            LogError(e);
        }

    }

    public async Task<string> PMToYear(DateOnly date)
    {
        try
        {
            // PM -> PY 🧸
            var yearMonthsFiles = await GetYear_MonthFilessAsync(date);

            var inverters = await GetInverters();

            var productions = new List<ProductionDto>();
            Parallel.ForEach(
                yearMonthsFiles.Where(x => x != null),
                month =>
                {
                    var prod = ProductionDto.FromJson(month.DataJson);
                    productions.Add(prod);
                }
            );
            productions = productions.OrderBy(x => x.TimeStamp).ToList();

            foreach (var inverter in inverters)
            {
                foreach (var production in productions)
                {
                    var totalProduction = production.Inverters
                                                    .Where(x => x.Id == inverter.Id)
                                                    .SelectMany(x => x.Production)
                                                    .Sum(x => (double)x.Value);
                    var updatedDate = new DateOnly(production.TimeStamp.Value.Year, production.TimeStamp.Value.Month, production.TimeStamp.Value.Day);
                    inverter.Production.Add(new DataPoint
                    {
                        Quality = 1,
                        TimeStamp = new DateTime(updatedDate, TimeOnly.MinValue, DateTimeKind.Utc),

                        Value = totalProduction,
                    });
                }
                inverter.Production = inverter.Production.OrderBy(x => x.TimeStamp).ToList();
            }

            var productionYear = new ProductionDto()
            {
                Inverters = inverters.ToList(),
                TimeType = (int)FileType.Year,
                TimeStamp = new DateTime((new DateOnly(date.Year, 1, 1)), TimeOnly.MinValue, DateTimeKind.Utc),
            };

            await ForcePublishAndRead($"py{date.Year}", ProductionDto.ToJson(productionYear));
            var jsonYearResult = await ReadBlobFile($"py{date.Year}"); // PR: ReadBlobFile is returned from ForcePublishAndRead above

            return jsonYearResult;
        }
        catch (Exception e)
        {
            LogError($"YearFailed : {date.Year}" + e.Message);
        }
        return null;
    }


    public async Task<string> YearToPT(DateOnly date)
    {
        try
        {
            //PY -> PT -> clouuudddd 🔥
            var productions = await GetYearsAsync();
            var inverters = ExtractInverters(
                (ProductionDto.FromJson(await ReadBlobFile("pt"))).Inverters
            );

            Parallel.ForEach(
                inverters,
                inverter =>
                {
                    foreach (var production in productions)
                    {
                        double totalProduction = 0;
                        var inv = production.Inverters.First(x => x.Id == inverter.Id);
                        double sum = (double)inv.Production.Sum(x => x.Value);
                        totalProduction += sum;

                        inverter.Production.Add(
                            new DataPoint()
                            {
                                Quality = 1,
                                TimeStamp =
                                new DateTime((new DateOnly(production.TimeStamp.Value.Year, 1, 1)), TimeOnly.MinValue, DateTimeKind.Utc),
                                Value = totalProduction,
                            }
                        );
                    }

                    inverter.Production = inverter
                                          .Production.OrderBy(x => x.TimeStamp)
                                          .ToList();
                }
            );

            var productionTotal = new ProductionDto()
            {
                Inverters = inverters.ToList(),
                TimeType = (int)FileType.Total,
                TimeStamp = new DateTime((new DateOnly(2014, 1, 1)), TimeOnly.MinValue, DateTimeKind.Utc)
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
        return await ForcePublishAndRead(fileName, productionJson);
    }

    public async Task<bool> UploadProductionAsync(ProductionDto production, FileType fileType)
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



    // private async Task<string> HandleMonth_DaysFiles(List<MonthProductionDTO> month)
    // {
    //     var options = new ParallelOptions
    //                   {
    //                       MaxDegreeOfParallelism = 20
    //                   };
    //
    //     var inverters = InitializeInverters(ProductionDto.FromJson(month.FirstOrDefault().DataJson).Inverters);
    //
    //     try
    //     {
    //         await Task.WhenAll(inverters.Select(async inverter =>
    //         {
    //             var totalMonthProduction = 0.0;
    //
    //             foreach (var day in month)
    //             {
    //                 var productionDay = ProductionDto.FromJson(day.DataJson);
    //                 var inverterProduction = productionDay.Inverters.FirstOrDefault(x => x.Id == inverter.Id);
    //
    //                 if (inverterProduction != null)
    //                 {
    //                     totalMonthProduction += inverterProduction.Production.Sum(x => (double)x.Value);
    //
    //                     inverter.Production.Add(new DataPoint
    //                                             {
    //                                                 Quality = 1,
    //                                                 TimeStamp = new DateTime(
    //                                                     productionDay.TimeStamp.Value.Year,
    //                                                     productionDay.TimeStamp.Value.Month,
    //                                                     productionDay.TimeStamp.Value.Day
    //                                                 ),
    //                                                 Value = totalMonthProduction,
    //                                             });
    //                 }
    //                 else
    //                 {
    //                 }
    //             }
    //
    //             inverter.Production = inverter.Production.OrderBy(x => x.TimeStamp).ToList();
    //             Log(
    //                 $"InvertId: {inverter.Id} \tproduced: {totalMonthProduction} \tDate: {inverter.Production[1].TimeStamp.Value.ToString()}");
    //         }));
    //     }
    //     catch (Exception e)
    //     {
    //         LogError(e);
    //     }
    //
    //     var production = new ProductionDto
    //                      {
    //                          TimeType = (int)FileType.Month,
    //                          TimeStamp = inverters.First().Production.First().TimeStamp,
    //                          Inverters = inverters.ToList(),
    //                      };
    //
    //     var result = await UploadProduction(production, FileType.Month);
    //
    //     return IsValidJson(result) ? result : null;
    // }
    //

    public async Task<string> ProcessInverterProductionAsync(List<MonthProductionDTO> month)
    {
        var inverters = await GetInverters();

        foreach (var inverter in inverters)
        {
            var totalMonthProduction = 0.0;
            DateTime? date = null;
            List<DataPoint> productions = new List<DataPoint>();
            foreach (var day in month)
            {
                var productionDay = ProductionDto.FromJson(day.DataJson);
                var inverterProduction = productionDay.Inverters.FirstOrDefault(x => x.Id == inverter.Id);

                if (inverterProduction != null)
                {
                    try
                    {
                        date = new DateTime(
                                                new DateOnly(productionDay.TimeStamp.Value.Year,
                                                productionDay.TimeStamp.Value.Month,
                                                productionDay.TimeStamp.Value.Day),
                                                TimeOnly.MinValue,
                                                DateTimeKind.Utc
                                            );

                        productions.Add(new DataPoint
                        {
                            Quality = 1,
                            TimeStamp = date,
                            Value = inverterProduction.Production.Sum(x => (double)x.Value),
                        });
                        totalMonthProduction += inverterProduction.Production.Sum(x => (double)x.Value);
                    }
                    catch (Exception e)
                    {
                        LogError(InstallationId + " ProcessInverterProductionAsync() -> " + e);
                    }
                }
            }
            productions.Add(new DataPoint
            {
                Quality = 1,
                TimeStamp = date,
                Value = totalMonthProduction,
            });

            inverter.Production = productions;
            inverter.Production = inverter.Production.OrderBy(x => x.TimeStamp).ToList();
            Log($"InvertId: {inverter.Id} \t" +
                $"produced: {totalMonthProduction} \t" +
                $"Date: {inverter.Production[1].TimeStamp.ToString()}");
        }

        var production = new ProductionDto
        {
            TimeType = (int)FileType.Month,
            TimeStamp = inverters.First().Production.First().TimeStamp,
            Inverters = inverters.ToList(),
        };

        var result = await UploadProduction(production, FileType.Month);
        return IsValidJson(result) ? result : null;
    }




    //public async Task<string> ProcessInverterProductionAsync(List<MonthProductionDTO> month)
    //{
    //    var inverters = await GetInverters();

    //    foreach (var inverter in inverters)
    //    {
    //        DateTime? date = null;
    //        List<DataPoint> productions = new List<DataPoint>();
    //        var totalMonthProduction = 0.0;
    //        foreach (var day in month)
    //        {
    //            var productionDay = ProductionDto.FromJson(day.DataJson);
    //            var inverterProduction = productionDay.Inverters.FirstOrDefault(x => x.Id == inverter.Id);

    //            if (inverterProduction != null)
    //            {
    //                try
    //                {
    //                    date = new DateTime(
    //                                            new DateOnly(productionDay.TimeStamp.Value.Year,
    //                                            productionDay.TimeStamp.Value.Month,
    //                                            productionDay.TimeStamp.Value.Day),
    //                                            TimeOnly.MinValue,
    //                                            DateTimeKind.Utc
    //                                        );
    //                    totalMonthProduction += inverterProduction.Production.Sum(x => (double)x.Value);

    //                    productions.Add(new DataPoint
    //                    {
    //                        Quality = 1,
    //                        TimeStamp = date,
    //                        Value = totalMonthProduction,
    //                    });
    //                }
    //                catch (Exception e)
    //                {
    //                    LogError(InstallationId + " ProcessInverterProductionAsync() -> " + e);
    //                }
    //            }
    //        }

    //        productions.Add(new DataPoint
    //        {
    //            Quality = 1,
    //            TimeStamp = date,
    //            Value = totalMonthProduction,
    //        });

    //        inverter.Production = productions;
    //        inverter.Production = inverter.Production.OrderBy(x => x.TimeStamp).ToList();
    //        Log($"InvertId: {inverter.Id} \t" +
    //            $"produced: {totalMonthProduction} \t" +
    //            $"Date: {inverter.Production[1].TimeStamp.ToString()}");
    //    }


    //    var result = await UploadProduction(production, FileType.Month);
    //    return IsValidJson(result) ? result : null;
    //}


    async Task<bool> DeleteAllYearFilesExceptDays(DateOnly date, FileType fileType = FileType.Day)
    {
        var yearBlolbBlocks = GetAllBlobsAsync().Result
                              .Where(blob =>
                                  blob.Name.Contains($"py{date.Year}") || blob.Name.Contains($"pm{date.Year}{date.Month}")
                                  && !blob.Name.Contains("pd")
                                  && !blob.Name.Contains("BackUp")
                              )
                              .ToList();

        var tasks = new List<Task>();
        foreach (var blob in yearBlolbBlocks)
        {
            tasks.Add(DeleteBlobFileIfExist(GetFileName(blob)));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            LogError("Could not delete all year files, year:" + date.Year);
            LogError(e.Message);
            return false;
        }

        return true;
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

public class MonthProductionDTO
{
    public FileType FileType { get; set; }

    public DateOnly Date { get; set; }

    public string DataJson { get; set; }
}