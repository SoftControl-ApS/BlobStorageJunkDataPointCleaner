using SharedLibrary.Models;
using static SharedLibrary.util.Util;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        string updatedJson = originalJson;
        if (string.IsNullOrEmpty(originalJson) || originalJson == "NOTFOUND")
        {
            updatedJson = await GenereateProduction(fileName);
        }

        await BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);

        return updatedJson;
    }

    private async Task<string> GenereateProduction(string fileName)
    {
        string updatedJson;
        string originalJson;
        ProductionDto fetchedData = null;
        int year = ExtractYearFromFileName(fileName);
        int tempYear = year;
        do
        {
            tempYear -= 1;
            originalJson = await ReadBlobFile($"py{tempYear}.json", $"py{tempYear}.zip", this.InstallationId);

        }
        while (originalJson == "NOTFOUND");
        fetchedData = ProductionDto.FromJson(originalJson);

        var inverters = InitializeInverters(fetchedData.Inverters);
        var updatedInverters = await UpdateInverterProductionData(inverters, year);
        var date = new DateOnly(ExtractYearFromFileName(fileName), 1, 1);
        var datetime = DateTime.SpecifyKind(new DateTime(date, new TimeOnly(0, 0, 0)), DateTimeKind.Utc);

        var updatedProduction = new ProductionDto()
        {
            Inverters = updatedInverters,
            TimeType = (int)FileType.Year,
            TimeStamp = datetime,

        };


        updatedJson = ProductionDto.ToJson(updatedProduction);
        if (!await CreateNewYearFile(updatedJson, fileName))
        {
            LogError($"Failed to update year file {fileName} for installation {InstallationId}");
            return null;
        }

        return updatedJson;
    }

    private async Task<bool> CreateNewYearFile(string json, string fileName)
    {
        var res = await CreateAndUploadBlobFile(json, fileName);
        return res;
    }


    private async Task<string> GenerateEmptyYearFile(DateOnly date, CancellationToken cancellationToken)
    {
        var datetime = new DateTime(date.Year, date.Month, 1);
        datetime = DateTime.SpecifyKind(datetime, DateTimeKind.Utc);
        var yearProduction = new ProductionDto()
        {
            TimeType = (int)FileType.Year,
            TimeStamp = datetime,
            Inverters = await GetInstallationInverters()
        };

        await Parallel.ForEachAsync(yearProduction.Inverters, cancellationToken, async (inverter, token) =>
        {
            for (int month = 1; month <= 12; month++)
            {
                inverter.Production.Add(new DataPoint()
                {
                    Quality = 0,
                    TimeStamp = DateTime.SpecifyKind(new DateTime(yearProduction.TimeStamp.Value.Year, month, 1), DateTimeKind.Utc)
                });
            }
        });

        var json = ProductionDto.ToJson(yearProduction);

        await WriteJson(json, $"py{date.Year}");

        await initBlobBlocks();

        return json;
    }

    private async Task<string> GenerateAndUploadEmptyYearFile(string fileName)
    {
        var year = ExtractYearFromFileName(fileName);

        return await GenerateEmptyYearFile(new DateOnly(year, 1, 1), CancellationToken.None);
    }


    async Task<List<Inverter>> GetInstallationInverters()
    {
        ProductionDto fetchedData = null;
        int tempYear = DateTime.Now.Year + 1;
        do
        {
            try
            {
                tempYear -= 1;

                var response = await ReadBlobFile($"py{tempYear}");
                fetchedData = ProductionDto.FromJson(response);
            }
            catch (Exception)
            {
                // Ignore}
            }

        }
        while (fetchedData == null);

        return InitializeInverters(fetchedData.Inverters);


    }

}