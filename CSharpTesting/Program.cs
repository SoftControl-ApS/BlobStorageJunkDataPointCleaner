using SharedLibrary;
using SharedLibrary.Azure;
using static SharedLibrary.util.Util;

namespace CSharpTesting;

class Program
{
    public static async Task Main(string[] args)
    {
        var installationId = "129";
        var containerName = "installations";
        var date = DateOnly.FromDateTime(new DateTime(2024, 1, 1));
        var energy = 3_600_000_000;
        SharedLibrary.ApplicationVariables.SetMaxEnergyInJoule(energy);


        Title($"Handling Installation {installationId}");
        Log($"ContainerName: {containerName}");
        Log($"date: {date.ToString()}");
        Log($"Max energy in Kwh: {energy / 36_00_000}");


        //var tasks = new List<Task>();

        //for (int year = date.Year; year >= 2020; year--)
        //{
        var instance = new AzureBlobCtrl(containerName, installationId);
        await instance.LetTheMagicHappen(date);
        //tasks.Add(instance.UpdateTotalFile(currentDate));
        //}

        //await Task.WhenAll(tasks);

    }
}