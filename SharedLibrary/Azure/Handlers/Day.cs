using System;
using System.Collections.Concurrent;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Microsoft.WindowsAzure.Storage.Blob;
using SharedLibrary.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    public async Task<bool> GenerateDayFile(DateOnly date)
    {
        var fileName = $"pd{date.Year}{date.Month:D2}{date.Day:D2}";
        var originalJson = await ReadBlobFile(fileName + ".json", fileName + ".zip", InstallationId);
        ProductionDto productionDto = null;
        if (originalJson != "NOTFOUND")
        {
            productionDto = ProductionDto.FromJson(originalJson);
            Parallel.ForEach(productionDto.Inverters, inv =>
            {
                foreach (var production in inv.Production)
                {
                    if (production.Value >= ApplicationVariables.MaxEnergyInJoules)
                    {
                        production.Value = 0;
                    }
                }
            });

            var updatedJson = ProductionDto.ToJson(productionDto);
            await BackupAndReplaceOriginalFile(fileName, null, updatedJson);
            return true;
        }
        else if (originalJson == "NOTFOUND")
        {
            var datetime = DateTime.SpecifyKind(new DateTime(date, new TimeOnly(0, 0, 0)), DateTimeKind.Utc);
            productionDto = new ProductionDto()
            {
                TimeType = (int)FileType.Day,
                TimeStamp = datetime,
                Inverters = await GetInstallationInverters()
            };

            Parallel.ForEach(productionDto.Inverters, inv =>
            {
                inv.Production = CreateEmptyDayDatapointList(date);
            });


            var updatedJson = ProductionDto.ToJson(productionDto);
            await CreateAndUploadBlobFile(updatedJson, fileName);

            return true;
        }

        return false;
    }

    //private List<DataPoint> CreateEmptyDayDatapointList(DateOnly date)
    //{
    //    var dataPoints = new List<DataPoint>();
    //    for (int i = 0; i < 24; i++)
    //    {
    //        dataPoints.Add(new DataPoint()
    //        {
    //            Quality = 0,
    //            TimeStamp = DateTime.SpecifyKind(new DateTime(date, new TimeOnly(i, 0, 0)), DateTimeKind.Utc),
    //            Value = 0

    //        });
    //    }

    //    return dataPoints;
    //}

    public async Task<string> HandleDayFiles(string fileName)
    {
        var originalJson = await ReadBlobFile(fileName);

        var productionDto = ProductionDto.FromJson(originalJson);
        bool didChange = false;

        foreach (var inv in productionDto.Inverters)
        {
            foreach (var production in inv.Production)
            {
                if (production.Value >= ApplicationVariables.MaxEnergyInJoules)
                {
                    production.Value = 0;
                    didChange = true;
                }
            }
        }

        if (didChange)
        {
            var updatedJson = ProductionDto.ToJson(productionDto);
            BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);
            return updatedJson;
        }
        else
        {
            return originalJson;
        }
    }

    public async Task<bool> RemoveAllJunkiesDayDataPoints()
    {
        Title("Remove All Junkies Day DataPoints", ConsoleColor.Cyan);
        var directory = GetContainerReference(this.ContainerName).GetDirectoryReference(InstallationId);
        BlobContinuationToken continuationToken = null;
        do
        {
            var results = await directory.ListBlobsSegmentedAsync(continuationToken);
            continuationToken = results.ContinuationToken;

            var blobs = results.Results
                               .OfType<CloudBlockBlob>()
                               .Where(blob => blob.Name.Contains("pd"))
                               .Where(blob => !blob.Name.Contains("BackUp"));

            Parallel.ForEach(blobs.Where(x => x.Name.ToLower().Contains("backup")), async blob =>
            {
                try
                {
                    await ProcessBlobAsync(blob);
                }
                catch (Exception ex)
                {
                    LogError($"Error deleting blob {blob.Name}: {ex.Message}");
                }
            });
            Log($"Cleaned day datapoints day files.");
        } while (continuationToken != null);

        return true;
    }

    private async Task<string> HandleDayFiles(DateOnly date)
    {
        var fileName = $"pd{date.Year}{date.Month:D2}{date.Day:D2}";
        var originalJson = await ReadBlobFile(fileName + ".json", fileName + ".zip", InstallationId);

        var productionDto = ProductionDto.FromJson(originalJson);

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
            var updatedJson = ProductionDto.ToJson(productionDto);
            await BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);
            return updatedJson;
        }
        else
        {
            return originalJson;
        }
    }

    private async Task<string> GenerateAndUploadEmptyDayFile(string fileName)
    {
        var date = ExtractDateFromFileName(fileName);

        return await GenerateAndUploadEmptyDayFile(date);
    }

    private async Task<string> GenerateAndUploadEmptyDayFile(DateOnly date)
    {
        var datetime = new DateTime(date, new TimeOnly(0, 0, 0));
        var dayProduction = new ProductionDto()
                            {
                                TimeType = (int)FileType.Day,
                                TimeStamp = datetime,
                                Inverters = await GetInstallationInverters()
                            };


        await Parallel.ForEachAsync(dayProduction.Inverters, CancellationToken.None, async (inverter, token) =>
        {
            for (int hour = 0; hour < 24; hour++)
            {
                var time = new TimeOnly(hour, 0, 0);
                inverter.Production.Add(new DataPoint()
                                        {
                                            Quality = 0,
                                            TimeStamp = new DateTime(date, time),
                                            Value = 0
                                        });
            }
        });


        var json = ProductionDto.ToJson(dayProduction);

        await UploadProduction(dayProduction, FileType.Day);

        return json;
    }

    async Task<string> GetDayFile(DateOnly date)
    {
        string fileName = $"pd{date.Year}{date.Year:D2}{date.Year:D2}";
        var json = await ReadBlobFile(fileName + ".json", fileName + ".zip", InstallationId);

        if (IsValidJson(json))
            return json;
        else
            return null;
    }


    async Task<HoodProduction?> GetDayFile(int currentMonth, int currentDay, DateOnly date)
    {
        string customTaskId = $"{currentMonth}/{currentDay}";

        string fileName = $"pd{date.Year}{currentMonth:D2}{currentDay:D2}";

        var json = await ReadBlobFile(fileName);

        if (IsValidJson(json))
        {
            try
            {
                var result = new HoodProduction()
                             {
                                 DataJson = json,
                                 Date = new DateOnly(date.Year, currentMonth, currentDay),
                                 FileType = FileType.Day,
                             };
                return result;
            }
            catch (Exception ex)
            {
                ApplicationVariables.FailedFiles.Add(new(fileName, "GetDayFile()"));
                LogError($"Task ID: {customTaskId} - Error Parsing Json: {fileName}");
                return null;
            }
        }

        ApplicationVariables.FailedFiles.Add(new(fileName, "GetDayFile()"));
        return null;
    }

    async Task<ConcurrentBag<HoodProduction>> GetYearDayFiles(DateOnly date)
    {
        ConcurrentBag<HoodProduction> dayFiles = new ConcurrentBag<HoodProduction>();
        List<Task> tasks = new List<Task>();

        for (int month = 1; month <= 12; month++)
        {
            int daysInMonth = DateTime.DaysInMonth(date.Year, month);
            for (int day = 1; day <= daysInMonth; day++)
            {
                int currentMonth = month;
                int currentDay = day;

                tasks.Add(Task.Run(async () =>
                {
                    HoodProduction? result = await GetDayFile(currentMonth, currentDay, date);
                    if (result != null)
                    {
                        dayFiles.Add(result);
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);
        return dayFiles;
    }

    public static bool IsValidJson(string json)
    {
        if (string.IsNullOrEmpty(json) ||
            (json == "NOTFOUND"))
        {
            return false;
        }

        return true;
    }
}