using System.Collections.Concurrent;
using Microsoft.WindowsAzure.Storage.Blob;
using SharedLibrary.Models;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    async Task<List<MonthProductionDTO>> GetYear_MonthFilessAsync(DateOnly date)
    {
        var allBlobs = await GetAllBlobsAsync();
        var monthsFiles = new List<MonthProductionDTO>();
        var tasks = new List<Task<MonthProductionDTO>>();
        var yearFiles = allBlobs.Where(blob => blob.Name.Contains($"pm{date.Year:D4}")).ToList();


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
}