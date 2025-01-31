using Microsoft.VisualBasic;
using SharedLibrary;
using SharedLibrary.Azure;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using static SharedLibrary.util.Util;

namespace CSharpTesting;

static class Program
{
    public static async Task Main(string[] args)
    {
        ConcurrentDictionary<string, string> failedFiles = new();
        var installationIds = new List<int>
                              {
                                  129, 102, 198 , 155, 150
                              };
        // for (int i = 24; i <= 54; i++)
        // {
        //     installationIds.Add(i);
        // }

        //Parallel.ForEach(installationIds, async installationID =>
        //await Parallel.ForEachAsync(installationIds, CancellationToken.None, // async (installationID, cancellationToken) =>
        foreach (var installationID in installationIds)
        {
            try
            {
                var installationId = installationID.ToString();
                var containerName = "installations";
                DateTime date = DateTime.Now;
                var energy = 3_600_000_000;
                ApplicationVariables.SetMaxEnergyInJoule(energy);
                Title($"Handling Installation {installationId}");
                Log($"ContainerName: {containerName}");
                Log($"date: {date.ToString()}");
                Log($"Max energy in Kwh: {energy / 36_00_000}");
                Stopwatch sw = new Stopwatch();
                sw.Start();

                var instance = new AzureBlobCtrl(containerName, installationId);
                for (int i = date.Year; i >= 2014; i--)
                {
                    var year = i;


                    try
                    {
                        if (await instance.CheckForExistingFiles(date))
                        {
                            Log($"No File found for this date {date.ToString()}");
                        }
                        else
                        {
                            await instance.LoadPT();
                            await instance.LetTheMagicHappen(new DateOnly(year, 1, 1));
                        }
                    }
                    catch (Exception e)
                    {
                        LogError(e);
                        failedFiles.TryAdd(installationId, e.Message);
                    }
                }

                var result = await instance.YearToPT(DateOnly.FromDateTime(date));

                sw.Stop();
                Log("Operatoin took" + sw.ElapsedMilliseconds / 1000 + "s");
                Log("-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_");
                Title("FINNISHED");

                _ = ApplicationVariables.FailedFiles.GroupBy(x => x.Name).OrderBy(x => x.Count()).ToList();
            }
            catch (Exception e)
            {
                failedFiles.TryAdd($"Somewhere", e.Message);
            }
        }

        sw.Stop();
        Log($"Operation took {sw.ElapsedMilliseconds / 1000}s");
        Title("FINISHED");

        if (failedFiles.Any())
        {
            Title("Failed Files");
            foreach (var s in failedFiles)
            {
                Console.WriteLine($"{s.Key} | {s.Value}");
            }
        }
    }
}