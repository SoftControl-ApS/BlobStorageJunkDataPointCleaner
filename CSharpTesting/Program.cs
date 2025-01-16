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

        SharedLibrary.ApplicationVariables.SetMaxEnergyInJoule(36_000_000);

        await new AzureBlobCtrl(containerName, installationId).UpDateAllFiles(date);
    }
}