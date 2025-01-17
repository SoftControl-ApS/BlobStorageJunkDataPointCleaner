using System.Reflection.Metadata;
using Microsoft.WindowsAzure.Storage.Blob;
using SharedLibrary.Models;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure;

public partial class AzureBlobCtrl
{
    private async Task<string> HandleDayFiles(string fileName)
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
            var updatedJson = ProductionDto.ToJson(productionDto);
            BackupAndReplaceOriginalFile(fileName, originalJson, updatedJson);
            return updatedJson;
        }
        else
        {
            return originalJson;
        }
    }

    public async Task<bool> RemoveAllJunkiesDayDataPoints()
    {
        Title("Remove All Junkies Day DataPoints", ConsoleColor.Cyan);
        var directory = GetContainerReference(this.ContainerName).GetDirectoryReference(InstallationId);
        BlobContinuationToken continuationToken = null;
        do
        {
            var results = await directory.ListBlobsSegmentedAsync(continuationToken);
            continuationToken = results.ContinuationToken;

            var blobs = results.Results
                               .OfType<CloudBlockBlob>()
                               .Where(blob => blob.Name.Contains("pd"))
                               .Where(blob => !blob.Name.Contains("BackUp"));

            Parallel.ForEach(blobs.Where(x => x.Name.ToLower().Contains("backup")), async blob =>
            {
                try
                {
                    await ProcessBlobAsync(blob);
                }
                catch (Exception ex)
                {
                    LogError($"Error deleting blob {blob.Name}: {ex.Message}");
                }
            });
            Log($"Cleaned day datapoints day files.");
        } while (continuationToken != null);

        return true;
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