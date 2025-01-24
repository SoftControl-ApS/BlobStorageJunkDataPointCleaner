using SharedLibrary;
using SharedLibrary.Azure;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using static SharedLibrary.util.Util;

namespace CSharpTesting;

static class Program
{
    public static async Task Main(string[] args)
    {
        var installationId = 102.ToString();
        var containerName = "installations";

        var date = new DateTime(2025, 1, 1, 1, 1, 1, DateTimeKind.Utc);

        var energy = 3_600_000_000;
        SharedLibrary.ApplicationVariables.SetMaxEnergyInJoule(energy);


        Title($"Handling Installation {installationId}");
        Log($"ContainerName: {containerName}");
        Log($"date: {date.ToString()}");
        Log($"Max energy in Kwh: {energy / 36_00_000}");


        var tasks = new List<Task>();
        for (int i = date.Year; i >= 2014; i--)
        {
            var year = i;
            tasks.Add(Task.Run(async () =>
            {
                var instance = new AzureBlobCtrl(containerName, installationId);
                return await instance.LetTheMagicHappen(new DateOnly(year, 1, 1));
            }));
        }

        Stopwatch sw = new Stopwatch();
        sw.Start();
        await Task.WhenAll(tasks);

        Log("Mission start");

        sw.Stop();
        Log("Mission took" + sw.ElapsedMilliseconds / 1000 + "s");
        Log("-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_");
        Title("FINNISHED");

        _ = SharedLibrary.ApplicationVariables.FailedFiles.GroupBy(x => x.Name).OrderBy(x => x.Count()).ToList();
    }
}