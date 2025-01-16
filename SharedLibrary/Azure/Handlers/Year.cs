using SharedLibrary.Models;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    private async Task<string> UpdateYearFiles(DateOnly date)
    {
        string fileName = $"py{date.Year}";
        return await UpdateYearFiles(fileName);
    }

    private async Task<string> UpdateYearFiles(string fileName)
    {
        var originalJson = await ReadBlobFile($"{fileName}.json", $"{fileName}.zip", this.InstallationId);

        var fetchedProductionData = ProductionDto.FromJson(originalJson);
        if (fetchedProductionData == null) return null;
        int year = ExtractYearFromFileName(fileName);
        var updatedInverters = await UpdateInverterProductionData(fetchedProductionData.Inverters, year);

        var updatedProduction = CreateUpdatedProductionDto(fetchedProductionData, updatedInverters);
        var updatedJson = ProductionDto.ToJson(updatedProduction);

        await BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);

        return updatedJson;
    }
}