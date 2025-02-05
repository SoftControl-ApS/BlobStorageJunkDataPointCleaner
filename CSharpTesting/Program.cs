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
        var installationIds = new List<int>(){
            #region Finished
            //1,2,14,276,105,106,107,108,109,
            //110,111,112,127,149,41,
            //3,4,5,7,8,20,468,9,10,11,13,15,
            //16,17,18,19,21,22,23,28,
            #endregion
            102
            // 542
            // 36,39,40,42,43,44,45,46,47,48,49,50,51,52,53,54,55,
            // 56,57,58,59,
            // 60,61,62,63,64,65,66,67,68,69,70,71,90,91,
            
            //92,93,94,95,96,97,100,123,151,154,313,315,714,
            //715,72,73,74,75,76,77,78,317,84,439,86,87,88,89,79,80,81,
            //82,98,99,102,113,114,116,117,119,130,137,141,144,120,122,
            //125,126,128,129,132,136,266,138,139,140,142,143,147,150,152,
            #region SKIP
            //155, 
            #endregion
            //156,210,272,157,158,159,160,161,162,163,164,165,166,167,
            //168,169,170,171,172,173,174,175,176,177,178,179,181,183,190,
            //202,205,206,207,208,209,593,595,819,822,197,198,199,200,201,
            //211,213,214,215,216,217,218,219,221,222,223,259,224,225,226,
            //229,235,284,302,238,239,242,243,249,244,245,246,247,248,253,254,
            //269,274,257,258,260,261,262,877,263,265,267,270,271,273,275,277,
            //279,280,281,282,285,307,293,294,

            //295,296,297,298,300,329,652,888,303,304,442,306,508,308,309,310,316,318,806,311,320,312,314,319,322,
            //323,325,327,328,330,420,333,341,336,339,340,342,343,344,345,346,347,348,349,351,352,355,356,471,357,358,
            //359,360,522,686,363,364,390,377,379,382,383,389,391,393,394,396,397,400,401,402,404,406,407,408,409,412,413,
            //428,415,416,417,418,424,425,426,427,429,431,432,433,435,436,437,438,440,443,603,445,446,588,448,449,451,452,453,
            //454,455,456,459,457,458,485,460,461,463,450,464,462,465,466,467,479,332,470,472,473,474,475,
            //476,477,480,481,482,484,486,487,669,492,493,494,495,496,497,489,498,499,500,501,502,503,504,505,506,507,509,
            //510,512,511,514,515,517,735,519,520,518,523,524,527,528,529,422,530,531,532,533,534,535,513,536,537,538,539,483,
            //540,542,544,569,570,572,632,771,541,543,545,547,550,551,552,283,553,554,555,556,557,558,559,560,561,562,563,564,
            //565,566,567,521,548,571,573,185,186,187,188,203,434,191,192,193,194,195,196,204,574,576,577,578,579,585,586,587,
            //589,590,591,189,594,596,597,598,469,599,600,601,602,604,605,606,607,609,610,611,612,613,614,616,615,617,618,619,620,
            //623,624,625,627,628,629,630,631,633,634,635,636,637,638,639,640,641,642,643,644,646,647,648,649,650,653,654,655,656,

            //657,658,660,659,662,663,664,665,666,667,621,705,661,670,671,672,673,674,675,676,677,678,679,680,681,683,682,684,685,687,688,689,691,690,692,693,694,695,696,697,698,699,701,702,703,704,706,700,708,709,710,711,712,713,645,716,717,718,719,720,726,727,731,733,734,736,737,738,739,740,742,744,745,746,747,748,749,750,751,444,753,754,755,756,757,758,759,760,761,762,764,765,766,767,768,770,769,772,778,779,780,788,789,790,791,792,793,794,795,796,882,801,802,803,804,807,809,810,811,812,813,814,815,817,818,820,805,549,823,824,825,826,491,827,828,829,830,831,832,833,834,835,836,837,843,844,846,847,741,848,845,850,852,854,855,707,871,878,879,856,857,860,868,870,872,873,874,875,876,880,869,887,889,
        };

        Stopwatch sw = new Stopwatch();
        sw.Start();
        var tasks = new List<Task>();
        foreach(var instID in installationIds)
        {
            tasks.Add(Run(instID));
        }
        await Task.WhenAll(tasks);
        sw.Stop();
        Log($"Operation took {sw.ElapsedMilliseconds / 1000}s");
        Title("FINISHED");
        Console.WriteLine("Run is complete. Press Enter to exit.");
        Console.ReadLine();
        Console.ReadLine();
        Console.ReadLine();
        Console.ReadLine();
    }

    public static async Task Run(int installationID)
    {
        try
        {
            var installationId = installationID.ToString();
            var containerName = "installations";
            DateTime date = DateTime.Now;
            var energy = 3_600_000_000;
            ApplicationVariables.SetMaxEnergyInJoule(energy);
            Console.WriteLine($"Handling Installation {installationId}");
            Console.WriteLine($"ContainerName: {containerName}");
            Console.WriteLine($"date: {date.ToString()}");
            Console.WriteLine($"Max energy in Kwh: {energy / 36_00_000}");

            var azureBlobCtrl = new AzureBlobCtrl(containerName, installationId);
            var solvePy = await azureBlobCtrl.SolvePy(date);
            if (solvePy)
            {
                for (int i = date.Year; i >= 2014; i--)
                {
                    var year = i;
                    var newDate = new DateTime(year, 1, 1);

                    try
                    {
                        var instance = new AzureBlobCtrl(containerName, installationId);
                        var foundFiles = await instance.CheckForExistingFiles(newDate);
                        if (!foundFiles)
                        {
                            continue;
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

                var result = await azureBlobCtrl.YearToPT(DateOnly.FromDateTime(date));
            }

            _ = ApplicationVariables.FailedFiles.GroupBy(x => x.Name).OrderBy(x => x.Count()).ToList();
        }
        catch (Exception e)
        {
            failedFiles.TryAdd($"Somewhere", e.Message);
        }


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