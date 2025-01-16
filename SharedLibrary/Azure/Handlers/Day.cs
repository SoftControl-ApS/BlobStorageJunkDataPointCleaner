using Microsoft.WindowsAzure.Storage.Blob;
using SharedLibrary.Models;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    private async Task<string> HandleDayFiles(CloudBlockBlob blobItem, string fileName)
    {
        var originalJson = await ReadBlobFile(fileName + ".json", fileName + ".zip", InstallationId);

        var productionDto = ProductionDto.FromJson(originalJson);
        bool didChange = false;

        foreach (var inv in productionDto.Inverters)
        {
            foreach (var production in inv.Production)
            {
                if (production.Value >= ApplicationVariables.MaxEnergyInJoules)
                {
                    production.Value = 0;
                    didChange = true;
                }
            }
        }

        if (didChange)
        {
            fileName = GetFileName(blobItem.Name);

            var updatedJson = ProductionDto.ToJson(productionDto);
            await WriteJson(json: originalJson, fileName: $"{fileName}_BackUp"); // Backup
            await DeleteBlobFile(fileName);                                      // Delete original
            await WriteJson(json: updatedJson, fileName: fileName);              // Uploadedre
            return originalJson;
        }
        else
        {
            return originalJson;
        }
    }

    private async Task<string> HandleDayFiles(DateOnly date)
    {
        var fileName = $"pd{date.Year}{date.Month:D2}{date.Day:D2}";
        var originalJson = await ReadBlobFile(fileName + ".json", fileName + ".zip", InstallationId);

        var productionDto = ProductionDto.FromJson(originalJson);

        bool didChange = false;

        Parallel.ForEach(productionDto.Inverters, inv =>
        {
            foreach (var production in inv.Production)
            {
                if (production.Value >= ApplicationVariables.MaxEnergyInJoules)
                {
                    production.Value = 0;
                    didChange = true;
                }
            }
        });

        if (didChange)
        {
            var updatedJson = ProductionDto.ToJson(productionDto);
            await BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);
            return updatedJson;
        }
        else
        {
            return originalJson;
        }
    }
}