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
                return;
            }

            var monthsDays = yearDays.OrderBy(x => x.Date).GroupBy(x => x.Date.Month).ToList();

            foreach (var month in monthsDays)
            {
                if(month.First().Date.Year == 2024)
                {

                }
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
}