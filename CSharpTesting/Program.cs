using CSharpTesting.Azure;
using Figgle;

namespace CSharpTesting;

class Program
{
    public static double MaxEnergyInJoules = 36_000_000; // 10kWh

    public static async Task Main(string[] args)
    {
        var file = "pd20250108.json";
        var zip = "pd20250108.zip";
        var installationId = "198";
        var containerName = "installations";

        var items = await AzureBlobCtrl.GetFilesFromFolderAsync(containerName, installationId);
    }
}