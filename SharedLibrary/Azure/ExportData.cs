using SharedLibrary.Models;
using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using static SharedLibrary.util.Util;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
        static CsvConfiguration csvConfig = new (CultureInfo.InvariantCulture)
                                            {
                                                Delimiter = ";",
                                                HasHeaderRecord = true,
                                            };

        private static string directoryCheck(string fileName)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string directoryPath = Path.Combine(desktopPath, "CSV");
            string outputFilePath = Path.Combine(directoryPath, $"{Path.GetFileNameWithoutExtension(fileName)}.csv");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            return outputFilePath;
        }

        public static bool ExportToCSV(ProductionDto production, string fileName)
        {
            var outputFilePath = directoryCheck(fileName);

            using (var writer = new StreamWriter(outputFilePath))
            using (var csv = new CsvWriter(writer, csvConfig))
            {
                csv.WriteField("TimeStamp");
                var invs = production.Inverters;
                invs.Reverse();
                foreach (var id in invs.Select(i => i.Id))
                {
                    csv.WriteField($"{id} | in Joule");
                    csv.WriteField($"{id} | in kWh");
                    csv.WriteField($"{id} | in Wh");
                    csv.WriteField($"");
                    csv.WriteField($"");
                }

                csv.NextRecord();

                int maxDataPoints = production.Inverters.Max(inv => inv.Production.Count);
                List<double> VALUES = new List<double>();
                for (int i = 0; i < maxDataPoints; i++)
                {
                    var timeStamp = production.Inverters.FirstOrDefault()?.Production.ElementAtOrDefault(i)?.TimeStamp;
                    csv.WriteField(timeStamp);

                    foreach (var inverter in production.Inverters)
                    {
                        var dataPoint = inverter.Production.ElementAtOrDefault(i);

                        if (inverter.Id == 611 && dataPoint.TimeStamp.Value.Hour == 11)
                            Console.WriteLine(dataPoint.Value);


                        double value = (double)dataPoint.Value;
                        double joules = value;
                        double kWh = joules / 3_600_000;
                        double Wh = kWh * 1_000;
                        VALUES.Add(value);
                        if (joules > 0)
                        {
                            Console.WriteLine("");
                        }

                        csv.WriteField(joules.ToString());
                        csv.WriteField(kWh.ToString());
                        csv.WriteField(Wh.ToString());
                        csv.WriteField("");
                        csv.WriteField("");
                    }

                    csv.NextRecord();
                }

                VALUES.Sort();
                var lowestvalue = VALUES.FirstOrDefault(x => x > 0);
                var highesvalue = VALUES.LastOrDefault(x => x > lowestvalue);

                csv.NextRecord();
                csv.WriteField("Lowest Value");
                csv.WriteField(lowestvalue);

                csv.NextRecord();

                csv.WriteField("Highest Value");
                csv.WriteField(highesvalue);

                csv.NextRecord();

                csv.WriteField("=");
                csv.WriteField(highesvalue - lowestvalue);

                csv.NextRecord();
                csv.NextRecord();
            }

            Message("Finished: " + fileName);
            Message("With File Path: " + outputFilePath);
            return true;
        }
    }
}