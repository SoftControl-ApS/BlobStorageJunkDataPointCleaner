using SharedLibrary;
using SharedLibrary.Azure;

namespace CSharpTesting;

class Program
{
    public static async Task Main(string[] args)
    {
        var installationId = "198";
        var containerName = "installations";
        var date = DateOnly.FromDateTime(new DateTime(2024, 12, 1));

        SharedLibrary.ApplicationVariables.SetMaxEnergyInJoule(3_600_000_000);
        var instance = new AzureBlobCtrl(containerName, installationId);
        if (await instance.RemoveAllJunkiesDayDataPoints())
            // {
            await instance.UpDateAllFiles(date);
        // }
    }
}