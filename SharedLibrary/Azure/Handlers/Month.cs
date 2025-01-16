using System.Collections.Concurrent;
using Microsoft.WindowsAzure.Storage.Blob;
using SharedLibrary.Models;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    private async Task<string> UpdateMonthFiles(DateOnly date)
    {
        string fileName = $"pm{date.Year}{date.Month:D2}";
        var originalJson = await ReadBlobFile($"{fileName}.json", $"{fileName}.zip", this.InstallationId);

        var fetchedProductionData = ProductionDto.FromJson(originalJson);
        if (fetchedProductionData == null) return null;

        var updatedInverters = await UpdateInverterProductionData(fetchedProductionData.Inverters, date);

        var updatedProduction = CreateUpdatedProductionDto(fetchedProductionData, updatedInverters);
        var updatedJson = ProductionDto.ToJson(updatedProduction);

        await BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);

        return updatedJson;
    }

    private async Task<Inverter> GetProductionMonth(DateOnly date, int InverterId)
    {
        string fileName = $"pm{date.Year}{date.Month:D2}";
        var jsonResponse = await ReadBlobFile(fileName);

        var production = ProductionDto.FromJson(jsonResponse);
        var inverter = production.Inverters.FirstOrDefault(i => i.Id == InverterId);
        return inverter;
    }

    private async Task<List<Inverter>> GetProductionMonthForAllInverters(DateOnly date)
    {
        var jsonResponse = await ReadBlobFile($"pm{date.Year}{date.Month}");
        return ProductionDto.FromJson(jsonResponse).Inverters;
    }

    private async Task<double> GetInverterTotalMonthProduction(DateOnly date, int inverterId)
    {
        var inverter = await GetInverterAllDayInMonthProduction(date, inverterId);
        // return inverter.Production.Sum(dataPoint => dataPoint.Value);
        return 0;
    }

    private async Task<double> GetInverterAllDayInMonthProduction(DateOnly date, int inverterId)
    {
        var inverter = await CalculateProductionMonth(date, inverterId);
        return inverter.Production.Sum(dataPoint => dataPoint.Value);
    }

    private async Task<Inverter> CalculateProductionMonth(DateOnly date, int InverterId)
    {
        string fileName = $"pd{date.Year}{date.Month:D2}";
        var DataPoints = new ConcurrentDictionary<string,List<DataPoint>>();

        //TODO
        var updatedInverter = new Inverter();

        int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
        string jsonResponse = String.Empty;
        double sum = 0;
        for (int day = 1; day <= daysInMonth; day++)
        {
            var updatedName = fileName + $"{day:D2}";
            jsonResponse = await ReadBlobFile(updatedName);
            var production = ProductionDto.FromJson(jsonResponse);
            var inverter = production.Inverters.First(x => x.Id == InverterId);
            DataPoints.TryAdd(updatedName,inverter.Production);
            sum += inverter.Production.Sum(x => x.Value);
        }

        // var inverter = production.Inverters.FirstOrDefault(i => i.Id == InverterId);
        return null;
    }
}