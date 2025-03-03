#pragma warning disable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Metadata;
using SharedLibrary.Models;
using System;
using static SharedLibrary.util.Util;
using static System.Net.Mime.MediaTypeNames;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    ConcurrentBag<DateTime> YearsToSkip = new();

    public async Task<string> SuyncPmToYear(DateOnly date)
    {
        try
        {
            // PM -> PY 🧸
            var yearMonthsFiles = await GetYear_MonthFilessAsync(date);

            if (yearMonthsFiles == null)
            {
                // var deleted = await DeleteBlobFileIfExist(GetFileName(date, FileType.Month));
                return string.Empty;
            }

            var inverters = new ConcurrentBag<Inverter>();

            foreach (var month in yearMonthsFiles)
            {

                try
                {
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    if (today.Year == month.Date.Year)
                    {
                        if (today.Month == month.Date.Month)
                        {
                            Console.WriteLine($"Wont handle current month:  {today}");
                            continue;
                        }
                    }

                }
                catch (Exception e)
                {
                    LogError("Installation ID: " + this.InstallationId + "\t" + e);

                    return "";
                }


                var invs = ExtractInverters(
                    ProductionDto.FromJson(yearMonthsFiles.First(x => !string.IsNullOrEmpty(x.DataJson)).DataJson)
                                 .Inverters);

                foreach (var inverrr in invs)
                {
                    if (!inverters.Any(i => i.Id == inverrr.Id))
                    {
                        inverters.Add(new Inverter()
                                      {
                                          Id = inverrr.Id,
                                          Production = new List<DataPoint>()
                                      });
                    }
                }
            }


            var productions = new ConcurrentBag<ProductionDto>();
            // var tasks = new List<Task>();
            foreach (var month in yearMonthsFiles.Where(x => x != null))
            {
                // tasks.Add(Task.Run(() =>
                // {
                var prod = ProductionDto.FromJson(month.DataJson);
                productions.Add(prod);
                // }));
            }

            //  await Task.WhenAll(tasks);

            var productionsList = productions.ToList().OrderBy(x => x.TimeStamp).ToList();
            foreach (var inverter in inverters)
            {
                foreach (var production in productionsList)
                {
                    var prodDate = production.TimeStamp.Value;
                    if (prodDate.Year == 2025 && prodDate.Month == 2)
                    {
                        var prod = ProductionDto.FromJson(await ReadBlobFile("pm202502"));
                        production.Inverters = new List<Inverter>();
                        production.Inverters = prod.Inverters;
                    }

                    var totalProduction = production.Inverters
                                                    .Where(x => x.Id == inverter.Id)
                                                    .SelectMany(x => x.Production)
                                                    .Sum(x => (double)x.Value);
                    var updatedDate = new DateTime(production.TimeStamp.Value.Year,
                        production.TimeStamp.Value.Month,
                        production.TimeStamp.Value.Day);
                    inverter.Production.Add(new DataPoint
                                            {
                                                Quality = 1,
                                                TimeStamp = updatedDate,
                                                Value = totalProduction,
                                            });
                }

                inverter.Production = inverter.Production.OrderBy(x => x.TimeStamp).ToList();
            }

            var productionYear = new ProductionDto()
                                 {
                                     Inverters = inverters.ToList(),
                                     TimeType = (int)FileType.Year,
                                     TimeStamp = new DateTime(date.Year, 1, 1),
                                 };


            var yearPRoductionSum = productionYear.Inverters.Sum(x => x.Production.Sum(x => x.Value));
            if (yearPRoductionSum <= 0)
            {
                YearsToSkip.Add(productionYear.TimeStamp.Value);
            }

            var jsonYearResult = await ForcePublishAndRead(GetFileName(date, FileType.Year),
                ProductionDto.ToJson(productionYear));
            return jsonYearResult;
        }
        catch (Exception e)
        {
            LogError($"InstallationId: {InstallationId} \t YearFailed : {date.Year}" + e.Message);
        }

        return null;
    }

    private async Task<List<MonthProductionDTO>> GetYear_MonthFilessAsync(DateOnly date)
    {
        var allBlobs = await GetAllBlobsAsync();
        if (allBlobs == null || !allBlobs.Any())
            return null;


        var yearFiles = allBlobs.Where(blob => blob.Name.Contains($"pm{date.Year:D4}"))
                                .Where(blob => blob.Name.Contains($"!pm{date.Year:D4}"))
                                .ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (today.Year == date.Year)
        {
            //Exclude days from the current month from being handled
            yearFiles = yearFiles.Where(blob => !blob.Name.Contains($"pm{today.Year:D4}{today.Month:D2}")).ToList();
        }



        var tasks = new List<Task<MonthProductionDTO>>();
        var monthsFiles = new List<MonthProductionDTO>();
        foreach (var year in yearFiles)
        {
            string filename = GetFileName(year);
            var productionDate = ExtractDateFromFileName(GetFileName(filename));

            tasks.Add(ReadBlobFile(filename).ContinueWith(result =>
            {
                if (result.Result != null)
                {
                    var some = new MonthProductionDTO()
                               {
                                   FileType = FileType.Year,
                                   Date = productionDate,
                                   DataJson = result.Result
                               };
                    return some;
                }

                return null;
            }));
        }

        var results = await Task.WhenAll(tasks);
        monthsFiles.AddRange(results);

        return monthsFiles;
    }
}