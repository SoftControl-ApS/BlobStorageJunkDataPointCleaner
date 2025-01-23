using SharedLibrary;
using SharedLibrary.Azure;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using static SharedLibrary.util.Util;

namespace CSharpTesting;

class Program
{
    public static async Task Main(string[] args)
    {
        var installationId = "129";
        var containerName = "installations";
        var date = DateTime.Now;
        var energy = 3_600_000_000;
        SharedLibrary.ApplicationVariables.SetMaxEnergyInJoule(energy);


        Title($"Handling Installation {installationId}");
        Log($"ContainerName: {containerName}");
        Log($"date: {date.ToString()}");
        Log($"Max energy in Kwh: {energy / 36_00_000}");


        var tasks = new List<Task>();
        for (int i = 2014; i <= date.Year; i++)
        {
            var year = i;
            tasks.Add(Task.Run(async () =>
            {
                var instance = new AzureBlobCtrl(containerName, installationId);
                return await instance.LetTheMagicHappen(new DateOnly(year, 1, 1));
            }));
        }

        Stopwatch sw = new Stopwatch(); sw.Start();

        Log("Mission start");
        await Task.WhenAll(tasks);
        Log("Mission ended");
        sw.Stop();

        Log("Mission took" + sw.ElapsedMilliseconds / 1000 + "s");

        Log("-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_");
        Title("FINNISHED");

        //tasks.Add(instance.UpdateTotalFile(currentDate));
        //}

        var a = SharedLibrary.ApplicationVariables.FailedFiles.Distinct().ToList();


    }
}