using SharedLibrary;
using SharedLibrary.Azure;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using SharedLibrary.Models;
using static SharedLibrary.util.Util;
using System.Collections.Concurrent;

namespace CSharpTesting;

static class Program
{
    public static Dictionary<string, string> Failed = new Dictionary<string, string>();

    public static async Task Main(string[] args)
    {
        var installationIds = new List<int>
                              {
                                  /*129, 102, 198 ,*/ 150
                              };
        // for (int i = 24; i <= 54; i++)
        // {
        //     installationIds.Add(i);
        // }

        Parallel.ForEach(installationIds, async installationID =>
        // await Parallel.ForEachAsync(installationIds, CancellationToken.None, // async (installationID, cancellationToken) =>
        //foreach (var installationID in installationIds)
        {
            try
            {
                var installationId = installationID.ToString();
                var containerName = "installations";
                var date = new DateTime(2025, 1, 1, 1, 1, 1, DateTimeKind.Utc);
                var energy = 3_600_000_000;
                ApplicationVariables.SetMaxEnergyInJoule(energy);
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
                        try
                        {
                            var instance = new AzureBlobCtrl(containerName, installationId);
                            if (!await instance.CheckForExistingFiles(date))
                            {
                                Log($"No FIle found for this date {date.ToString()}");
                                return null;
                            }
                            else
                            {
                                await instance.LoadPT();
                                return await instance.LetTheMagicHappen(new DateOnly(year, 1, 1));
                            }
                        }
                        catch (Exception e)
                        {
                            LogError(e);
                            Failed.TryAdd(installationId, e.Message);
                        }

                        return null;
                    }));
                }

                Stopwatch sw = new Stopwatch();
                sw.Start();
                Task.WaitAll(tasks);

                sw.Stop();
                Log("Operatoin took" + sw.ElapsedMilliseconds / 1000 + "s");
                Log("-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_");
                Title("FINNISHED");

                _ = ApplicationVariables.FailedFiles.GroupBy(x => x.Name).OrderBy(x => x.Count()).ToList();
            }
            catch (Exception e)
            {
                Failed.Add($"Exception{Guid.NewGuid().ToString()}", e.Message);
            }
        }
         );

        string directoryPath = @"C:\Users\KevinBamwesa\Desktop";
        string filePath = Path.Combine(directoryPath, $"{Guid.NewGuid()}.txt");

        // Check if the directory exists, if not, create it
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        using (TextWriter tw = new StreamWriter(filePath))
        {
            foreach (var s in Failed)
            {
                await tw.WriteAsync(s.Key);
                await tw.WriteAsync("");
                await tw.WriteLineAsync(s.Value);
            }
        }
    }
}