using Microsoft.VisualBasic;
using SharedLibrary;
using SharedLibrary.Azure;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using static SharedLibrary.util.Util;

namespace CSharpTesting;

static partial class Program
{
    static ConcurrentDictionary<string, string> failedFiles = new();

    static async Task Main(string[] args)
    {
        List<int> DoneInstallation = new List<int>(){
              1,2,3,
              4, 5,7,
              8,9,14,10,
              11,13,15,16,17,18,19,
              
              
              101,102,150,155,199,272,
        };

        var cts = new CancellationTokenSource();
        var feedbackTask = Task.Run(() => PrintDots(cts.Token));

        // Start the main operation
        Stopwatch sw = new Stopwatch();
        sw.Start();
        // var tasks = new List<Task>();
        foreach (var instID in installationIds)
        {
            if(DoneInstallation.Contains(instID))
            continue;
            await CheckAndDelay();
            await Run(instID);
        }

        // await Task.WhenAll(tasks);
        sw.Stop();
        Log($"Operation took {sw.ElapsedMilliseconds / 1000}s");
        Title("FINISHED");

        // Stop the feedback thread


        // Handle failed files if any
        if (failedFiles.Any())
        {
            Title("Failed Files");
            foreach (var s in failedFiles)
            {
                Console.WriteLine($"{s.Key} | {s.Value}");
            }
        }
        cts.Cancel();
        await feedbackTask;

        Console.WriteLine("Run is complete. Press Enter to exit.");
        Console.ReadLine();
        Console.ReadLine();
        Console.ReadLine();
    }

    static async Task CheckAndDelay()
{
    DateTime now = DateTime.Now;
    int minutesToNextHour = 60 - now.Minute;

    // If the current time is within 5 minutes before the next hour (xx:55 - xx:59)
    if (minutesToNextHour <= 5)
    {
        DateTime waitUntil = now.AddMinutes(minutesToNextHour + 5);
        TimeSpan delay = waitUntil - DateTime.Now;

        Console.WriteLine($"Waiting until {waitUntil} to continue...");
        await Task.Delay(delay);
    }
    }


    static void PrintDots(CancellationToken token)
    {
        Random random = new Random();
        while (!token.IsCancellationRequested)
        {
            int dotCount = random.Next(1, 6); // Generate a random number between 1 and 5
            for (int i = 0; i < dotCount; i++)
            {
                Console.Write(".\t");
            }
            Thread.Sleep(5000); // Wait for 500 milliseconds before printing the next set of dots
        }
    }

    public static async Task Run(int installationID)
    {
        try
        {
            var installationId = installationID.ToString();
            var containerName = SharedLibrary.ApplicationVariables.AzureBlobContainerReference;
            DateTime date = DateTime.Now;
            // var energy = 3_600_000_000;
            var energy = 540_000_000;
            Console.WriteLine($"Handling Installation {installationId}");
            Console.WriteLine($"ContainerName: {containerName}");
            Console.WriteLine($"date: {date.Day}-{date.Month}-{date.Year}");
            Console.WriteLine($"Max energy in Kwh: {energy / 36_00_000}");

            var azureBlobCtrl = new AzureBlobCtrl(containerName, installationId);
            var instance = new AzureBlobCtrl(containerName, installationId);
            var solvePy = await azureBlobCtrl.SolvePy(date);
            if (solvePy)
            {
                for (int i = date.Year; i >= 2014; i--)
                {
                    var year = i;
                    var newDate = new DateTime(year, 1, 1);

                    try
                    {
                        var foundFiles = await instance.CheckForExistingFiles(newDate);
                        if (!foundFiles)
                        {
                            continue;
                        }
                        else
                        {
                            await instance.Run(new DateOnly(year, 1, 1));
                        }
                    }
                    catch (Exception e)
                    {
                        LogError(e);
                        failedFiles.TryAdd(installationId, e.Message);
                    }
                }
                var result = await instance.YearToPT(DateOnly.FromDateTime(date));
            }

            _ = ApplicationVariables.FailedFiles.GroupBy(x => x.Name).OrderBy(x => x.Count()).ToList();
        }
        catch (Exception e)
        {
            failedFiles.TryAdd($"Somewhere", e.Message);
        }
    }
}