using SharedLibrary;
using SharedLibrary.Azure;

namespace CSharpTesting;

class Program
{
    public static async Task Main(string[] args)
    {
        var installationId = "198";
        var containerName = "installations";

        SharedLibrary.ApplicationVariables.SetMaxEnergyInJoule(36_000_000);

        var items = await new  AzureBlobCtrl(containerName, installationId).RemoveJunkieDataPoints();
    }
}