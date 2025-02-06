#pragma warning disable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Metadata;
using SharedLibrary.Models;
using System;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    private ConcurrentDictionary<string, string> _editedFiles = new ConcurrentDictionary<string, string>();
    private string _ptJson = string.Empty;
    private ProductionDto _ptProductionDto;

    public async Task LoadPT()
    {
        try
        {
            _ptJson = await ReadBlobFile("pt");
            _ptProductionDto = ProductionDto.FromJson(_ptJson);
        }
        catch (Exception e)
        {
            LogError($"InstallationId + {InstallationId} \tPT could not be loaded");
            throw;
        }
    }

    public async Task<bool> CheckForExistingFiles(DateTime date)
    {
        try
        {
            var allCloudBlobs = (await GetAllBlobsAsync());
            var blobs = allCloudBlobs
                        .Where(blob => blob != null)
                        .Where(blob => blob.Name.Contains($"pd{date.Year}") ||
                                       blob.Name.Contains($"pm{date.Year}") ||
                                       blob.Name.Contains($"pm{date.Year}"))
                        .ToList();

            try
            {
                var pt = allCloudBlobs.First(b => b.Name.Contains("pt"));
                if (pt == null)
                {
                    LogError($"InstallationId: {InstallationId} \tCritical : PT was null for installation: " +
                             InstallationId);
                    Console.WriteLine($"InstallationId: {InstallationId} \tPress a key to continue");
                    Console.ReadLine();
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"InstallationId: {InstallationId} \tNo power total file found");
                Console.WriteLine($"InstallationId: {InstallationId} \tPress a key to continue");
                Console.ReadLine();
            }


            if (!blobs.Any())
            {
                Log($"No File found for this date {date.ToString()} \t instID: {InstallationId}");
                return false;
            }
            else
            {
                if (blobs.Count == 1)
                {
                    if (blobs.FirstOrDefault().Name.Contains($"py{date.Year}"))
                    {
                        Log(
                            $"InstallationId: {InstallationId} \tNo usefull File found for this date {date.ToString()}");
                        return false;
                    }
                }
            }

            return blobs.Any();
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public async Task<bool> SolvePy(DateTime date)
    {
        try
        {
            var blobs = await GetAllBlobsAsync();
            var productionYears = blobs.Where(blob => blob.Name.Contains("py") ||
                                                      blob.Name.Contains("pd") ||
                                                      blob.Name.Contains("pm") ||
                                                      blob.Name.Contains("pt")).ToList();
            if (!productionYears.Any())
            {
                LogError($"InstallationId: {InstallationId} \tNo inverters found");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            LogError($"InstallationId: {InstallationId} \tFailed / not supported + " + InstallationId);
            return false;
        }
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

    public async Task<string> LetTheMagicHappen(DateOnly date)
    {
        // var duplicateResult = await DuplicateBlobFolder(ContainerName, InstallationId);
        // if (!duplicateResult)
        // {
        //     LogError("Critical Error: Cannot duplicate this installation");
        //     return null;
        // }

        //await DeleteAllYearFilesExceptDays(date);
        bool hasfile = await CheckForDayFiles(date);
        if (!hasfile)
        {
            Log($"InstallationId: {InstallationId} \t" + "Year" + date.Year + " Will not be handled");
        }
        else
        {
            await CleanYear_AllDaysFiles(date);
        }

        await UpdatePDtoPM(date);
        await PMToYear(date);
        Log($"InstallationId: {InstallationId} \tPM - > Year DONE {date.Year}");
        return "success";
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
                        if (!( i == val - 1))
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
                        var result = await BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);
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

    #region PD -> PM

    public async Task UpdatePDtoPM(DateOnly date)
    {
        try
        {
            var yearDays = await GetYearDayFiles(date);

            if (!yearDays.Any())
            {
                return;
            }

            var monthsDays = yearDays.OrderBy(x => x.Date).GroupBy(x => x.Date.Month).ToList();

            foreach (var month in monthsDays)
            {
                var monthGroup = month.ToList();
                var result = await ConvertProductionDayToProductionMonthAsync(monthGroup);
            }
        }
        catch (Exception e)
        {
            LogError($"InstallationId: {InstallationId} \t" + e);
        }

        Log(
            $"InstallationId: {InstallationId} \tPD -> PM -> clouuudddd \ud83d\udd25 DONE {date.Month}/{date.Year}");
    }

    public async Task<string> ConvertProductionDayToProductionMonthAsync(List<MonthProductionDTO> month)
    {
        var inverters = ExtractInverters(
            ProductionDto.FromJson(month.First(x => !string.IsNullOrEmpty(x.DataJson)).DataJson).Inverters
        );

        foreach (var inverter in inverters)
        {
            var totalMonthProduction = 0.0;
            DateTime? date = new DateTime?();
            List<DataPoint> productions = new List<DataPoint>();
            foreach (var day in month)
            {
                var productionDay = ProductionDto.FromJson(day.DataJson);
                var inverterProduction = productionDay.Inverters.Single(x => x.Id == inverter.Id);

                if (inverterProduction != null)
                {
                    try
                    {
                        date = productionDay.TimeStamp;
                        productions.Add(new DataPoint
                                        {
                                            Quality = 1,
                                            TimeStamp = new DateTime(date.Value.Year, date.Value.Month,
                                                date.Value.Day),
                                            Value = inverterProduction.Production.Sum(x => (double)x.Value),
                                        });

                        // Add total day production to the total month production
                        totalMonthProduction += inverterProduction.Production.Sum(x => (double)x.Value);
                    }
                    catch (Exception e)
                    {
                        LogError($"InstallationId: {InstallationId} \t" + " ProcessInverterProductionAsync() -> " +
                                 e);
                    }
                }
            }

            inverter.Production = productions.Distinct().ToList();
        }

        var firstInvTime = inverters.First().Production.First().TimeStamp;
        var production = new ProductionDto
                         {
                             TimeType = (int)FileType.Month,
                             TimeStamp = new DateTime(firstInvTime.Value.Year, firstInvTime.Value.Month, 1),
                             Inverters = inverters.ToList(),
                         };
        try
        {
            var result = await UploadProduction(production, FileType.Month);

            return result;
        }
        catch (Exception e)
        {
            LogError($"InstallationId: {InstallationId} \tProduction Month could not be uploaded ¨\t" +
                     InstallationId +
                     "\tDate: " +
                     month.First().Date.ToString());
            return null;
        }
    }

    #endregion

    #region PM -> YEAR

    public async Task<string> PMToYear(DateOnly date)
    {
        try
        {
            // PM -> PY 🧸
            var yearMonthsFiles = await GetYear_MonthFilessAsync(date);

            if (yearMonthsFiles == null)
            {
                var deleted = await DeleteBlobFileIfExist(GetFileName(date, FileType.Month));
                return string.Empty;
            }

            var inverters = ExtractInverters(
                ProductionDto.FromJson(yearMonthsFiles.First(x => !string.IsNullOrEmpty(x.DataJson)).DataJson)
                             .Inverters
            );

            var productions = new ConcurrentBag<ProductionDto>();
            var tasks = new List<Task>();
            foreach (var month in yearMonthsFiles.Where(x => x != null))
            {
                tasks.Add(Task.Run(() =>
                {
                    var prod = ProductionDto.FromJson(month.DataJson);
                    productions.Add(prod);
                }));
            }

            await Task.WhenAll(tasks);

            var productionsList = productions.ToList().OrderBy(x => x.TimeStamp).ToList();
            foreach (var inverter in inverters)
            {
                foreach (var production in productionsList)
                {
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

            var jsonYearResult = await ForcePublishAndRead($"py{productionYear.TimeStamp.Value.Year}",
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


        var yearFiles = allBlobs.Where(blob => blob.Name.Contains($"pm{date.Year:D4}")).ToList();

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

    #endregion

    #region Yeart -> PT

    public async Task<string> YearToPT(DateOnly date)
    {
        try
        {
            //PY -> PT -> clouuudddd 🔥
            var productions = await GetYearsAsync();
            var inverters = ExtractInverters(productions.First().Inverters);

            foreach (var inverter in inverters)
            {
                foreach (var production in productions)
                {
                    double totalProduction = 0;
                    var inv = production.Inverters.First(x => x.Id == inverter.Id);
                    double sum = (double)inv.Production.Sum(x => x.Value);
                    totalProduction += sum;

                    if (totalProduction > 0)
                        inverter.Production.Add(
                            new DataPoint()
                            {
                                Quality = 1,
                                TimeStamp =
                                    new DateTime(production.TimeStamp.Value.Year, 1, 1),
                                Value = totalProduction,
                            }
                        );
                }

                inverter.Production = inverter
                                      .Production.OrderBy(x => x.TimeStamp)
                                      .ToList();
            }

            var productionTotal = new ProductionDto()
                                  {
                                      Inverters = inverters.ToList(),
                                      TimeType = (int)FileType.Total,
                                      TimeStamp = new DateTime((new DateOnly(2014, 1, 1)), TimeOnly.MinValue,
                                          DateTimeKind.Local)
                                  };

            var res = string.Empty;
            var productionJson = ProductionDto.ToJson(productionTotal);
            res = await ForcePublishAndRead("pt", productionJson);

            //res = await UploadProduction(productionTotal, FileType.Total);

            Log($"InstallationId: {InstallationId} \tYear -> PT DONE {date.Year}");

            return res;
        }
        catch (Exception e)
        {
            LogError($"InstallationId: {InstallationId} \tTotalFile failed :" + e.Message);
        }

        return null;
    }

    #endregion

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

    async Task<bool> DeleteAllYearFilesExceptDays(DateOnly date)
    {
        var yearBlolbBlocks = GetAllBlobsAsync().Result
                                                .Where(blob => blob.Name.Contains($"py{date.Year}")
                                                               && blob.Name.Contains($"pm{date.Year}")
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
            LogError($"InstallationId: {InstallationId} \tCould not delete all year files, year:" + date.Year);
            LogError($"InstallationId: {InstallationId} \t" + e.Message);
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