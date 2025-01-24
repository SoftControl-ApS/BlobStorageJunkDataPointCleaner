using System.Collections.Concurrent;
using Microsoft.WindowsAzure.Storage.Blob;
using SharedLibrary.Models;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    //private async Task<string> UpdateMonthFiles(DateOnly date)
    //{
    //    string fileName = $"pm{date.Year}{date.Month:D2}";
    //    var originalJson = await ReadBlobFile($"{fileName}.json", $"{fileName}.zip", this.InstallationId);

    //    var fetchedProductionData = ProductionDto.FromJson(originalJson);
    //    if (fetchedProductionData == null) return null;

    //    var updatedInverters = await UpdateInverterProductionData(fetchedProductionData.Inverters, date);

    //    var updatedProduction = CreateUpdatedProductionDto(fetchedProductionData, updatedInverters);
    //    var updatedJson = ProductionDto.ToJson(updatedProduction);

    //    await BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);

    //    return updatedJson;
    //}

    //private async Task<List<DataPoint>> GetProductionMonth(DateOnly date, int InverterId)
    //{
    //    string fileName = $"pm{date.Year}{date.Month:D2}";
    //    var jsonResponse = await ReadBlobFile(fileName);

    //    var production = ProductionDto.FromJson(jsonResponse);
    //    var inverter = production.Inverters.FirstOrDefault(i => i.Id == InverterId).Production;
    //    return inverter;
    //}

    //private async Task<double?> GetInverterTotalMonthProduction(DateOnly date, int inverterId)
    //{
    //    var inverter = await GetTotalMonthProduction(date, inverterId);
    //    try
    //    {
    //        var sum = inverter.Sum(dataPoint => dataPoint.Value);
    //        return sum;
    //    }
    //    catch (Exception e)
    //    {
    //        LogError(e.Message);
    //        return null;
    //    }
    //}

    //private async Task<ConcurrentDictionary<string, double?>> CalculateProductionMonth(DateOnly date, int InverterId)
    //{
    //    string fileName = $"pm{date.Year}{date.Month:D2}";
    //    var DataPoints = new ConcurrentDictionary<string, double?>();
    //    int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
    //    string jsonResponse = String.Empty;


    //    for (int day = 1; day <= daysInMonth; day++)
    //    {
    //        var updatedName = fileName + $"{day:D2}";
    //        jsonResponse = await ReadBlobFile(updatedName);

    //        var production = ProductionDto.FromJson(jsonResponse);
    //        var inverter = production.Inverters.FirstOrDefault(x => x.Id == InverterId);
    //        if (inverter != null)
    //        {
    //            DataPoints.TryAdd(updatedName, inverter.Production.Sum(x => x.Value));
    //        }
    //        else
    //        {
    //            LogError($"Could not Calculate month {date.Month} year {date.Year} total Production for {InverterId}");
    //        }
    //    }

    //    return DataPoints;
    //}


    //private async Task<ConcurrentDictionary<string, double?>> GetTotalMonthProduction(DateOnly date, int InverterId)
    //{
    //    string fileName = $"pd{date.Year}{date.Month:D2}";
    //    var DataPoints = new ConcurrentDictionary<string, double?>();
    //    int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
    //    string jsonResponse = String.Empty;


    //    for (int day = 1; day <= daysInMonth; day++)
    //    {
    //        var updatedName = fileName + $"{day:D2}";
    //        jsonResponse = await ReadBlobFile(updatedName);
    //        if (jsonResponse == "NOTFOUND")
    //        {
    //            jsonResponse = await GenerateProductionDayFile(updatedName);
    //        }

    //        var production = ProductionDto.FromJson(jsonResponse);
    //        var inverter = production.Inverters.FirstOrDefault(x => x.Id == InverterId);
    //        if (inverter != null)
    //        {
    //            DataPoints.TryAdd(updatedName, inverter.Production.Sum(x => x.Value));
    //        }
    //        else
    //        {
    //            LogError($"Could not Calculate month {date.Month} year {date.Year} total Production for {InverterId}");
    //        }
    //    }

    //    return DataPoints;
    //}

    //private async Task<string> GenerateProductionDayFile(string filename)
    //{
    //    DateOnly date = ExtractDateFromFileName(filename);

    //    if (await GenerateDayFile(date))
    //    {
    //        return await ReadBlobFile(filename);
    //    }

    //    return "null";
    //}

    async Task<List<HoodProduction>> GetYear_MonthFilessAsync(DateOnly date)
    {
        var monthsFiles = new List<HoodProduction>();
        var tasks = new List<Task<HoodProduction>>();

        for (int month = 1; month <= 12; month++)
        {
            var requestDate = new DateOnly(date.Year, month, 1);
            string filename = GetFileName(requestDate, FileType.Month);

            // Start the asynchronous read operation and add the task to the list
            tasks.Add(ReadBlobFile(filename).ContinueWith(result =>
            {
                return new HoodProduction()
                {
                    FileType = FileType.Year,
                    Date = requestDate,
                    DataJson = result.Result
                };
            }));
        }


        var results = await Task.WhenAll(tasks);
        monthsFiles.AddRange(results);

        return monthsFiles;
    }

    //async Task<HoodProduction?> GenerateYearFile(int currentMonth, DateOnly date)
    //{
    //    string customTaskId = $"{currentMonth}";

    //    string fileName = $"pm{date.Year}{currentMonth:D2}";
    //    var json = await ReadBlobFile(fileName + ".json", fileName + ".zip", InstallationId);

    //    if (IsValidJson(json))
    //    {
    //        try
    //        {
    //            var result = new HoodProduction()
    //            {
    //                DataJson = json,
    //                Date = new DateOnly(date.Year, date.Month, 1),
    //                FileType = FileType.Month,
    //            };
    //            return result;
    //        }
    //        catch (Exception ex)
    //        {
    //            ApplicationVariables.FailedFiles.Add(fileName);
    //            LogError($"Task ID: {customTaskId} - Error Parsing Json: {fileName}");
    //            LogError(ex.Message, ConsoleColor.DarkRed);
    //            return null;
    //        }
    //    }
    //    else
    //    {
    //        LogError("filename: " + fileName + " was not found");
    //    }

    //    ApplicationVariables.FailedFiles.Add(fileName);
    //    LogError($"Task ID: {customTaskId} - Invalid Json: fileName {fileName} Content:\n{json}");
    //    return null;
    //}
}