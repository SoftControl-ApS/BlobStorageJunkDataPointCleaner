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
    //public async Task<bool> GenerateDayFile(DateOnly date)
    //{
    //    var fileName = $"pd{date.Year}{date.Month:D2}{date.Day:D2}";
    //    var originalJson = await ReadBlobFile(fileName);
    ProductionDto productionDto = null;
    //    if (originalJson != "NOTFOUND")
    //    {
    //        productionDto = ProductionDto.FromJson(originalJson);
    //        Parallel.ForEach(productionDto.Inverters, inv =>
    //        {
    //            foreach (var production in inv.Production)
    //            {
    //                if (production.Value >= ApplicationVariables.MaxEnergyInJoules)
    //                {
    //                    production.Value = 0;
    //                }
    //            }
    //        });

    //        var updatedJson = ProductionDto.ToJson(productionDto);
    //        await BackupAndReplaceOriginalFile(fileName, null, updatedJson);
    //        return true;
    //    }
    //    else if (originalJson == "NOTFOUND")
    //    {
    //        var datetime = DateTime.SpecifyKind(new DateTime(date, new TimeOnly(0, 0, 0)), DateTimeKind.Utc);
    //        productionDto = new ProductionDto()
    //        {
    //            TimeType = (int)FileType.Day,
    //            TimeStamp = datetime,
    //            Inverters = await GetInstallationInverters()
    //        };

    //        Parallel.ForEach(productionDto.Inverters, inv =>
    //        {
    //            inv.Production = CreateEmptyDayDatapointList(date);
    //        });


    //        var updatedJson = ProductionDto.ToJson(productionDto);
    //        await CreateAndUploadBlobFile(updatedJson, fileName);

    //        return true;
    //    }

    //    return false;
    //}

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

    // private async Task<string> HandleDayFiles(DateOnly date)
    // {
    //     var fileName = $"pd{date.Year}{date.Month:D2}{date.Day:D2}";
    //     var originalJson = await ReadBlobFile(fileName + ".json", fileName + ".zip", InstallationId);
    //
    //     var productionDto = ProductionDto.FromJson(originalJson);
    //
    //     bool didChange = false;
    //
    //     Parallel.ForEach(productionDto.Inverters, inv =>
    //     {
    //         foreach (var production in inv.Production)
    //         {
    //             if (production.Value >= ApplicationVariables.MaxEnergyInJoules)
    //             {
    //                 production.Value = 0;
    //                 didChange = true;
    //             }
    //         }
    //     });
    //
    //     if (didChange)
    //     {
    //         var updatedJson = ProductionDto.ToJson(productionDto);
    //         await BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);
    //         return updatedJson;
    //     }
    //     else
    //     {
    //         return originalJson;
    //     }
    // }

    private async Task<string> GenerateAndUploadEmptyDayFile(string fileName)
    {
        var date = ExtractDateFromFileName(fileName);

        return await GenerateAndUploadEmptyDayFile(date);
    }

    private async Task<string> GenerateAndUploadEmptyDayFile(DateOnly date)
    {
        //var datetime = new DateTime(date, new TimeOnly(0, 0, 0));
        var datetime = new DateTime(date, TimeOnly.MinValue, DateTimeKind.Utc);

        var dayProduction = new ProductionDto()
        {
            TimeType = (int)FileType.Day,
            TimeStamp = datetime,
            Inverters = (await GetInverters()).ToList()
        };


        Parallel.ForEach(dayProduction.Inverters, inverter =>
        {
            for (int hour = 0; hour < 24; hour++)
            {
                var time = new TimeOnly(hour, 0, 0);
                inverter.Production.Add(new DataPoint()
                {
                    Quality = 0,
                    TimeStamp = new DateTime(date, time, DateTimeKind.Utc),
                    Value = 0
                });
            }
        });

        await DeleteBlobFileIfExist(GetFileName(date, FileType.Day));
        var json = ProductionDto.ToJson(dayProduction);
        bool result = await UploadProductionAsync(dayProduction, FileType.Day);
        if (result)
            return json;
        return "Successfully Generated";
    }

    // async Task<string> GetDayFile(DateOnly date)
    // {
    //     string fileName = $"pd{date.Year}{date.Year:D2}{date.Year:D2}";
    //     var json = await ReadBlobFile(fileName + ".json", fileName + ".zip", InstallationId);
    //
    //     if (IsValidJson(json))
    //         return json;
    //     else
    //         return null;
    // }


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

        var json = await ReadBlobFile(fileName);

        if (IsValidJson(json))
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
                ApplicationVariables.FailedFiles.Add(new(fileName, "GetDayFile()"));
                LogError($"Task ID: {customTaskId} - Error Parsing Json: {fileName}");
                return null;
            }
        }
        else
        {
            LogError(fileName + "\tGetDayFile() : Invalid Json");
        }

        ApplicationVariables.FailedFiles.Add(new(fileName, "GetDayFile()"));
        return null;
    }

    async Task<ConcurrentBag<MonthProductionDTO>> GetYearDayFiles(DateOnly date)
    {
        ConcurrentBag<MonthProductionDTO> dayFiles = new ConcurrentBag<MonthProductionDTO>();
        List<Task> tasks = new List<Task>();

        var allBlobs = await GetAllBlobsAsync();
        for (int month = 1; month <= 12; month++)
        {
            var filteredBlobs = allBlobs.Where(blob => blob.Name.Contains($"pd{date.Year}{month:D2}")).ToList();

            tasks.Add(Task.Run(async () =>
            {
                var innerTasks = filteredBlobs.Select(async blob =>
                {
                    MonthProductionDTO? result = await GetDayFile(GetFileName(blob));
                    if (result != null)
                    {
                        dayFiles.Add(result);
                    }
                });

                await Task.WhenAll(innerTasks);
            }));
        }

        await Task.WhenAll(tasks);
        return dayFiles;
    }

    public static bool IsValidJson(string json)
    {
        if (json == "NOTFOUND")
        {
            return false;
        }
        else if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var prod = JsonConvert.DeserializeObject<ProductionDto>(json, Converter.Settings);

                if (prod != null)
                    return true;
                else
                    return false;
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }
        return false;
    }
}