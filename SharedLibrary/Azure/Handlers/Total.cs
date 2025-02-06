using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using SharedLibrary.Models;
using SharedLibrary.util;
using System.Collections.Concurrent;
using static SharedLibrary.util.Util;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    async Task<ConcurrentBag<ProductionDto>> GetYearsAsync()
    {
        var yearBlolbBlocks = GetAllBlobsAsync().Result
        .Where(blob =>
        blob.Name.Contains($"py")
        && !blob.Name.Contains("_backup"))
        .ToList();

        var productions = new ConcurrentBag<ProductionDto>();
        foreach (var yearblob in yearBlolbBlocks.Where(b => true).ToList())
        {
            string? response = await ReadBlobFile(GetFileName(yearblob));
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