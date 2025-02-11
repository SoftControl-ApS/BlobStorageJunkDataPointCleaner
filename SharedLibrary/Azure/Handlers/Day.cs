using System;
using System.Collections.Concurrent;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SharedLibrary.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    async Task<MonthProductionDTO?> GetDayFile(DateOnly date)
    {
        return await GetDayFile(date.Month, date.Day, date);
    }

    async Task<MonthProductionDTO?> GetDayFile(string fileName)
    {
        var date = ExtractDateFromFileName(fileName);
        return await GetDayFile(date);
    }

    async Task<MonthProductionDTO?> GetDayFile(int currentMonth, int currentDay, DateOnly date)
    {
        string customTaskId = $"{currentMonth}/{currentDay}";

        string fileName = $"pd{date.Year}{currentMonth:D2}{currentDay:D2}";

        if (!await BlobExistsAsync(fileName))
        {
            return null;
        }

        var json = await ReadBlobFile(fileName);

        if (json != null)
        {
            try
            {
                var result = new MonthProductionDTO()
                             {
                                 DataJson = json,
                                 Date = new DateOnly(date.Year, currentMonth, currentDay),
                                 FileType = FileType.Day,
                             };
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Task ID: {customTaskId} - Error Parsing Json: {fileName}");
                return null;
            }
        }

        return null;
    }

    async Task<ConcurrentBag<MonthProductionDTO>> GetYearDayFiles(DateOnly date)
    {
        ConcurrentBag<MonthProductionDTO> dayFiles = new ConcurrentBag<MonthProductionDTO>();
        List<Task> tasks = new List<Task>();

        var allBlobs = await GetAllBlobsAsync();
        for (int month = 1; month <= 12; month++)
        {
            var filteredBlobs = allBlobs.Where(blob => blob.Name.Contains($"pd{date.Year}{month:D2}")
                                                       && !blob.Name.ToLower().Contains($"_backup")).ToList();
            filteredBlobs = filteredBlobs.OrderBy(b => b.Name).ToList();

            tasks.Add(Task.Run(async () =>
            {
                var innerTasks = filteredBlobs.Select(async blob =>
                {
                    if (blob != null)
                    {
                        MonthProductionDTO? result = await GetDayFile(GetFileName(blob));
                        if (result != null)
                        {
                            dayFiles.Add(result);
                        }
                    }
                });

                await Task.WhenAll(innerTasks);
            }));
        }

        await Task.WhenAll(tasks);
        var result = new ConcurrentBag<MonthProductionDTO>(dayFiles.OrderByDescending(x => x.Date));
        return result;
    }

    async Task<bool> CheckForDayFiles(DateOnly date)
    {
        var blobs = await GetAllBlobsAsync();
        var yearDayBlobs = blobs.Where(x => x.Name.Contains($"pd{date.Year}")).ToList();
        if (!yearDayBlobs.Any())
        {
            Log($"InstallationId + {InstallationId} \tNo Day Files found for this date {date.ToString()}");
            return false;
        }

        return true;
    }

    public async Task<bool> CleanYear_AllDaysFiles(DateOnly date)
    {
        var blobs = _blobs
                    .Where(blob => blob.Name.Contains($"pd{date.Year}"))
                    .Where(blob => !blob.Name.ToLower().Contains("_backup"))
                    .ToList();
        foreach (var blob in blobs)
        {
            var fileName = GetFileName(blob.Name);
            var originalJson = await ReadBlobFile(fileName);
            var Edate = ExtractDateFromFileName(fileName);
            if (Edate.Year < 2014)
            {
                continue;
            }

            if (fileName.Contains("backup") || fileName.Contains("_backup"))
                continue;

            var filedate = ExtractDateFromFileName(fileName);
            if (filedate > new DateOnly(2025, 01, 20))
                continue;

            List<Inverter> updatedInverters = new();

            var wasUpdated = false;
            if (originalJson != null)
            {
                var originalProduction = ProductionDto.FromJson(originalJson);
                if (originalProduction != null)
                {
                    foreach (var inverter in originalProduction.Inverters.Where(x => true))
                    {
                        if (inverter == null)
                        {
                            continue;
                        }

                        foreach (var production in inverter.Production.Where(x => true))
                        {
                            if (production == null)
                            {
                                continue;
                            }

                            if (production.Value >= ApplicationVariables.MaxEnergyInJoules)
                            {
                                Log($"InstallationId: {InstallationId} \t" +
                                    $"FileName: {fileName}\t" +
                                    $"Inverter: {inverter.Id}\t" +
                                    $"Value: {production.Value} date {production.TimeStamp.Value.ToString()}"
                                );

                                production.Value = 0;
                                production.Quality = 1;
                                _editedFiles.TryAdd(fileName, fileName);
                                wasUpdated = true;
                            }

                            if (updatedInverters.Any(x => x.Id == inverter.Id))
                            {
                                updatedInverters.Single(x => x.Id == inverter.Id).Production.Add(production);
                            }
                            else
                            {
                                updatedInverters.Add(
                                    new Inverter()
                                    {
                                        Id = inverter.Id,
                                        Production = new List<DataPoint> { production }
                                    });
                            }
                        }
                    }
                }

                try
                {
                    int val = new Random().Next(1, 10);
                    for (int i = 0; i < val; i++)
                    {
                        if (!(i == val - 1))
                        {
                            Console.Write(".");
                        }
                        else
                        {
                            Console.WriteLine("");
                        }
                    }

                    if (wasUpdated)
                    {
                        Console.WriteLine(
                            $"InstallationId: {InstallationId} \tFileName: {fileName}\t" + "Updating File");
                        var updatedProduction = new ProductionDto()
                                                {
                                                    TimeStamp = new DateTime(
                                                        originalProduction.TimeStamp.Value.Year,
                                                        originalProduction.TimeStamp.Value.Month,
                                                        originalProduction.TimeStamp.Value.Day),
                                                    TimeType = originalProduction.TimeType,
                                                    Inverters = updatedInverters,
                                                };
                        var updatedJson = ProductionDto.ToJson(updatedProduction);
                        var result = await ForcePublish(fileName, updatedJson);
                        if (!result)
                            LogError($"InstallationId: {InstallationId} \tCould Not Update filename: " +
                                     fileName);
                    }
                }
                catch (Exception e)
                {
                    LogError($"InstallationId {InstallationId}\t Could not update file: {fileName}\t" + e);
                }
            }
        }

        if (_editedFiles.Any())
        {
            Log($"InstallationId: {InstallationId} \tRemoved All junks Day DataPoints {date.ToString()}",
                ConsoleColor.DarkBlue);
        }

        return true;
    }
}