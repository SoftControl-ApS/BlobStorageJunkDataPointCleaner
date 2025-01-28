using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using SharedLibrary.Models;
using SharedLibrary.util;
using System.Collections.Concurrent;
using static SharedLibrary.util.Util;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    //public async Task<string> UpdateTotalFile(DateOnly date)
    //{
    //    string fileName = "pt";
    //    var originalJson = await ReadBlobFile($"{fileName}.json", $"{fileName}.zip", this.InstallationId);
    //    if (originalJson == "NOTFOUND")
    //    {
    //        LogError("failed to UpdateTotalFile, since total file was not foud : " + InstallationId);
    //        return null;
    //    }
    //    var fetchedProductionData = ProductionDto.FromJson(originalJson);
    //    var updatedInverters = await GetUpdatedInverterYearProductionData(fetchedProductionData.Inverters, date);

    //    var updatedProduction = FinalHandle(fetchedProductionData, updatedInverters, date);
    //    var updatedJson = ProductionDto.ToJson(updatedProduction);

    //    await BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);

    //    Title("Finished year" + date.ToString());

    //    return updatedJson;
    //}

    //ProductionDto FinalHandle(ProductionDto oldProduction, List<Inverter> updatedInverters, DateOnly date)
    //{

    //    foreach (var inverter in updatedInverters)
    //    {
    //        double yearTotal = inverter.Production
    //            .Where(x => x.Value.HasValue && !double.IsNaN(x.Value.Value))
    //            .Sum(x => x.Value.Value);

    //        if (inverter.Production.Any(x => x.TimeStamp.Value.Year == date.Year))
    //        {
    //            oldProduction.Inverters.Where(inv => inv.Id == inverter.Id)
    //            .FirstOrDefault()
    //            .Production
    //            .Where(x => x.TimeStamp.Value.Year == date.Year)
    //            .FirstOrDefault()
    //            .Value = yearTotal;

    //        }
    //        {
    //            oldProduction.Inverters.Where(inv => inv.Id == inverter.Id)
    //            .FirstOrDefault()
    //            .Production
    //            .Add(new DataPoint()
    //            {
    //                Quality = 1,
    //                TimeStamp = new DateTime(date, TimeOnly.MinValue, DateTimeKind.Utc),
    //                Value = yearTotal
    //            });
    //        }
    //    }

    //    ProductionDto productionTotal = new ProductionDto()
    //    {
    //        TimeStamp = oldProduction.TimeStamp,
    //        TimeType = oldProduction.TimeType,
    //        Inverters = oldProduction.Inverters
    //    };

    //    return productionTotal;
    //}

    async Task<ConcurrentBag<ProductionDto>> GetYearsAsync()
    {
        var yearBlolbBlocks = GetAllBlobsAsync().Result
        .Where(blob =>
        blob.Name.Contains($"py")
        && !blob.Name.Contains("BackUp")
        ).ToList();


        var productions = new ConcurrentBag<ProductionDto>();
        foreach (var yearblob in yearBlolbBlocks)
        {
            string response = await ReadBlobFile(GetFileName(yearblob));

            if (response != null)
            {
                var yearProd = ProductionDto.FromJson(response);
                productions.Add(yearProd);
            }
            else
            {
                LogError("Could not handle file: " + GetFileName(yearblob));
            }

        }
        return productions;
    }
}