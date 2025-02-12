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
    
    public async Task<string> Run(DateOnly date)
    {
        // var duplicateResult = await DuplicateBlobFolder(ContainerName, InstallationId);
        // if (!duplicateResult)
        // {
        //     LogError("Critical Error: Cannot duplicate this installation");
        //     return null;
        // }

        //await DeleteAllYearFilesExceptDays(date);
        // bool hasfile = await CheckForDayFiles(date);
        // if (!hasfile)
        // {
        //     Log($"InstallationId: {InstallationId} \t" + "Year" + date.Year + " Will not be handled");
        // }
        // else
        // {
        //     await CleanYear_AllDaysFiles(date);
        // }

        await SyncPmWithPd(date);
        await SuyncPmToYear(date);
        Log($"InstallationId: {InstallationId} \tPM - > Year DONE {date.Year}");
        return "success";
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
                Log($"No File found for this date{date.Day}-{date.Month}-{date.Year} \t instID: {InstallationId}");
                return false;
            }
            else
            {
                if (blobs.Count == 1)
                {
                    if (blobs.FirstOrDefault().Name.Contains($"py{date.Year}"))
                    {
                        Log(
                            $"InstallationId: {InstallationId} \tNo usefull File found for this date {date.Day}-{date.Month}-{date.Year}");
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
}

public class MonthProductionDTO
{
    public FileType FileType { get; set; }

    public DateOnly Date { get; set; }

    public string DataJson { get; set; }
}