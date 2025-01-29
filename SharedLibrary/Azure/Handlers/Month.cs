using System.Collections.Concurrent;
using Microsoft.WindowsAzure.Storage.Blob;
using SharedLibrary.Models;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    async Task<List<MonthProductionDTO>> GetYear_MonthFilesAsync(DateOnly date)
    {
        var allBlobs = await GetAllBlobsAsync();

        var yearFiles = allBlobs
            .Where(blob => blob.Name.Contains($"pm{date.Year:D4}"))
            .ToList();

        var tasks = yearFiles
            .Where(file => file != null)
            .Select(async file =>
            {
                string filename = GetFileName(file);
                var productionDate = ExtractDateFromFileName(filename);

                var dataJson = await ReadBlobFile(filename);
                if (dataJson != null)
                {
                    return new MonthProductionDTO
                    {
                        FileType = FileType.Year,
                        Date = productionDate,
                        DataJson = dataJson
                    };
                }
                return null;
            });

        var results = await Task.WhenAll(tasks);
        return results.Where(result => result != null).ToList();
    }
}