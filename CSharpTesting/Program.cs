using SharedLibrary;
using SharedLibrary.Azure;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using static SharedLibrary.util.Util;

namespace CSharpTesting;

static class Program
{
    private static ConcurrentDictionary<string, string> failedFiles = new();

    public static async Task Main(string[] args)
    {
        var installationIds = new List<int>
                              {
                                  103, 104, 148, 153, 241, 12, 85, 118, 124, 6, 101, 133, 135, 227, 228, 230, 231, 232,
                                  233, 234, 236, 237, 240, 250, 251, 252, 255, 256, 268, 301, 326, 331, 334, 335, 337,
                                  350, 365, 546, 821, 212, 220, 286, 264, 287, 288, 289, 290, 291, 292, 299, 411, 858,
                                  305, 399, 321, 324, 419, 338, 353, 354, 361, 525, 366, 386, 395, 373, 374, 375, 378,
                                  380, 381, 384, 385, 387, 388, 392, 398, 403, 405, 410, 414, 421, 423, 478, 430, 376,
                                  447, 441, 488, 490, 516, 526, 575, 580, 581, 582, 583, 584, 608, 786, 787, 568, 732,
                                  182, 592, 622, 626, 651, 668, 743, 721, 722, 723, 730, 724, 728, 725, 729, 752, 362,
                                  773, 774, 775, 776, 777, 781, 782, 783, 784, 785, 816, 763, 808, 797, 862, 798, 863,
                                  799, 864, 800, 861, 838, 839, 840, 841, 842, 849, 851, 853, 865, 866, 867, 859,
                              };
        var energy = 3_600_000_000;
        var date = new DateTime(DateOnly.FromDateTime(DateTime.Now), TimeOnly.MinValue, DateTimeKind.Utc);
        var containerName = "installations";
        ApplicationVariables.SetMaxEnergyInJoule(energy);
        Log($"ContainerName: {containerName}");
        Log($"date: {date}");
        Log($"Max energy in Kwh: {energy / 3_600_000}");

        Stopwatch sw = new Stopwatch();
        sw.Start();
        await Parallel.ForEachAsync(installationIds, async (installationID, _) =>
        {
            try
            {
                var installationId = installationID.ToString();
                Log($"Handling Installation {installationId}");

                var instance = new AzureBlobCtrl(containerName, installationId);
                for (int i = date.Year; i >= 2014; i--)
                {
                    try
                    {
                        if (await instance.CheckForExistingFiles(date))
                        {
                            await instance.LetTheMagicHappen(new DateOnly(i, 1, 1));
                        }
                    }
                    catch (Exception e)
                    {
                        LogError(e);
                        failedFiles.TryAdd(installationId, e.Message);
                    }
                }

                await instance.YearToPT(DateOnly.FromDateTime(date));
            }
            catch (Exception e)
            {
                failedFiles.TryAdd($"Somewhere", e.Message);
            }
        });

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