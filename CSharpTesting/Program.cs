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
    static ConcurrentDictionary<string, string> failedFiles = new();

    static async Task Main(string[] args)
    {
        var installationIds = new List<int>()
        {
            //1,2,3,
            // 4, 
            7
            // 5,7,
            //8,9,14,10,
            // 11,13,15,16,17,18,19,
            #region DONE
            // 101,102,150,155,156,
            // 199,272,
            // 1,2,3,4,5,7, 8,9,10,
            #endregion DONE
            // 11, 13, 14,15,16,17,18,
            // 19,20,21,22,
            // 23,

            
                                // 28,36,39,40,41,
                                // 42,43,44,45,46,
                                // 47,48,49,50,51,
                                // 52,53,54,55,56,
                                // 57,58,59,60,61,
                                // 62,63,64,65,66,
                                // 67,68,69,70,71,
                                // 72,73,74,75,76,
                                // 77,78,79,80,81,
                                // 82,84,86,87,88,
                                // 89,90,91,92,93,
                                // 94,95,96,97,98,
                                // 99,100,102,105,106,
                                // 107,108,109,110,111,
                                // 112,113,114,116,117,
                                // 119,120,122,123,125,
                                // 126,127,128,129,130,
                                // 132,136,137,138,139,
                                // 140,141,142,143,144,
                                // 147,149,150,151,152,
                                // 154,155,156,157,158,
                                // 159,160,161,162,163,
                                // 164,165,166,167,168,
                                // 169,170,171,172,173,
                                // 174,175,176,177,178,
                                // 179,181,183,185,186,
                                // 187,188,189,190,191,
                                // 192,193,194,195,196,
                                // 197,198,199,200,201,
                                // 202,203,204,205,206,
                                // 207,208,209,210,211,
                                // 213,214,215,216,217,
                                // 218,219,221,222,223,
                                // 224,225,226,229,235,
                                // 238,239,242,243,244,
                                // 245,246,247,248,249,
                                // 253,254,257,258,259,
                                // 260,261,262,263,265,
                                // 266,267,269,270,271,
                                // 272,273,274,275,276,
                                // 277,279,280,281,282,
                                // 283,284,285,293,294,
                                // 295,296,297,298,300,
                                // 302,303,304,306,307,
                                // 308,309,310,311,312,
                                // 313,314,315,316,317,
                                // 318,319,320,322,323,
                                // 325,327,328,329,330,
                                // 333,336,339,340,341,
                                // 342,343,344,345,346,
                                // 347,348,349,351,352,
                                // 355,356,357,358,359,
                                // 360,363,364,377,379,
                                // 382,383,389,390,391,
                                // 393,394,396,397,400,
                                // 401,402,404,406,407,
                                // 408,409,412,413,415,
                                // 416,417,418,420,422,
                                // 424,425,426,427,428,
                                // 429,431,432,433,434,
                                // 435,436,437,438,439,
                                // 440,442,443,444,445,
                                // 446,448,449,450,451,
                                // 452,453,454,455,456,
                                // 457,458,459,460,461,
                                // 462,463,464,465,466,
                                // 467,468,469,470,471,
                                // 472,473,474,475,476,
                                // 477,479,480,481,482,
                                // 483,484,485,486,487,
                                // 489,491,492,493,494,
                                // 495,496,497,498,499,
                                // 500,501,502,503,504,
                                // 505,506,507,508,509,
                                // 510,511,512,513,514,
                                // 515,517,518,519,520,
                                // 521,522,523,524,527,
                                // 528,529,530,531,532,
                                // 533,534,535,536,537,
                                // 538,539,540,541,542,
                                // 543,544,545,547,548,
                                // 549,550,551,552,553,
                                // 554,555,556,557,558,
                                // 559,560,561,562,563,
                                // 564,565,566,567,569,
                                // 570,571,572,573,574,
                                // 576,577,578,579,585,
                                // 586,587,588,589,590,
                                // 591,593,594,595,596,
                                // 597,598,599,600,601,
                                // 602,603,604,605,606,
                                // 607,609,610,611,612,
                                // 613,614,615,616,617,
                                // 618,619,620,621,623,
                                // 624,625,627,628,629,
                                // 630,631,632,633,634,
                                // 635,636,637,638,639,
                                // 640,641,642,643,644,
                                // 645,646,647,648,649,
                                // 650,652,653,654,655,
                                // 656,657,658,659,660,
                                // 661,662,663,664,665,
                                // 666,667,669,670,671,
                                // 672,673,674,675,676,
                                // 677,678,679,680,681,
                                // 682,683,684,685,686,
                                // 687,688,689,690,691,
                                // 692,693,694,695,696,
                                // 697,698,699,700,701,
                                // 702,703,704,705,706,
                                // 708,709,710,711,712,
                                // 713,714,715,716,717,
                                // 718,719,720,726,727,
                                // 731,733,734,735,736,
                                // 737,738,739,740,741,
                                // 742,744,745,746,747,
                                // 748,749,750,751,753,
                                // 754,755,756,757,758,
                                // 759,760,761,762,764,
                                // 765,766,767,768,769,
                                // 770,771,772,778,779,
                                // 780,788,789,790,791,
                                // 792,793,794,795,796,
                                // 801,802,803,804,805,
                                // 806,807,809,810,811,
                                // 812,813,814,815,817,
                                // 818,819,820,822,823,
                                // 824,825,826,827,828,
                                // 829,830,831,832,833,
                                // 834,835,836,837,843,
                                // 844,845,846,847,848,
                                // 850,852,854,855,856,
                                // 857,860,868,869,870,
                                // 871,872,873,874,875,
                                // 876,877,878,879,880,
                                // 887,888,889,890,891,
                                // 892,893,894,895,896,
                                // 897,898,899,900,901,
                                // 902,

        };

        var cts = new CancellationTokenSource();
        var feedbackTask = Task.Run(() => PrintDots(cts.Token));

        // Start the main operation
        Stopwatch sw = new Stopwatch();
        sw.Start();
        // var tasks = new List<Task>();
        foreach (var instID in installationIds)
        {
            // await CheckAndDelay();
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
                Console.Write(".");
            }
            Console.WriteLine(); // Move to the next line after printing the dots
            Thread.Sleep(500); // Wait for 500 milliseconds before printing the next set of dots
        }
    }

    public static async Task Run(int installationID)
    {
        try
        {
            var installationId = installationID.ToString();
            var containerName = "installations";
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