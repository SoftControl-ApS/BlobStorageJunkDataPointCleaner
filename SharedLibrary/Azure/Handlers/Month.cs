using System.Collections.Concurrent;
using Microsoft.WindowsAzure.Storage.Blob;
using SharedLibrary.Models;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    public async Task SyncPmWithPd(DateOnly date)
    {
        try
        {
            var yearDays = await GetYearDayFiles(date);

            if (!yearDays.Any())
            {
                Log($"(Warning) InstallationId : {InstallationId}\t No days files found for year {date.Year}");
                return;
            }

            var monthsDays = yearDays.OrderBy(x => x.Date).GroupBy(x => x.Date.Month).ToList();

            foreach (var month in monthsDays)
            {
                Console.WriteLine("Working ... ");
                var monthGroup = month.ToList();
                var result = await ConvertProductionDayToProductionMonthAsync(monthGroup);
                Console.WriteLine($"Finished date: {month.First().Date.ToString()}");
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
            ProductionDto.FromJson(month.First(x => !string.IsNullOrEmpty(x.DataJson)).DataJson).Inverters!
        );

        foreach (var inverter in inverters)
        {
            var totalMonthProduction = 0.0;
            DateTime? date = new DateTime?();
            List<DataPoint> productions = new List<DataPoint>();
            
            foreach (var day in month)
            {
                if(day.Date >= new DateOnly(DateTime.Today.Year,DateTime.Today.Month,DateTime.Today.Day))
                    break;

                var productionDay = ProductionDto.FromJson(day.DataJson);
                 Inverter inverterProduction = null;
                try{
                    inverterProduction = productionDay.Inverters.Single(x => x.Id == inverter.Id);
                }
                catch(Exception e)
                {
                    LogError($"InstallationID {InstallationId} \t"+
                    "Exception When getting Inverter.Single()\t" +
                    $"Date: {date}");
                }

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

                    var A = inverterProduction.Production.Where(x => x.TimeStamp.Value.Date > DateTime.Today);
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
            DateOnly? date = month.First().Date;
            if (date == null)
                LogError($"InstallationID {InstallationId} \tdate was null, ConvertProductionDayTOMonthASYNC");

            LogError($"InstallationId: {InstallationId} \tProduction Month could not be uploaded ¨\tInstallationID: " +
                     InstallationId +
                     "\tDate: " + $"{date?.Day}-{date?.Month}-{date?.Year}");
            return null;
        }
    }
}