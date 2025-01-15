using SharedLibrary.Models;
using Figgle;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {

        private static string GetFileName(string blobName)
        {
            var fileName = Path.GetFileName(blobName);

            if (fileName.EndsWith(".zip"))
            {
                fileName = fileName.Substring(0, fileName.Length - 4);
            }
            else if (fileName.EndsWith(".json"))
            {
                fileName = fileName.Substring(0, fileName.Length - 5);
            }

            return fileName;
        }
        private static async Task<string> ProcessBlobAsync(IListBlobItem item, List<string> filedList, int installationId)
        {
            if (item is not CloudBlockBlob blobItem)
                return String.Empty;

            filedList.Add(blobItem.Name);

            if (!await blobItem.ExistsAsync())
            {
                Log($"Filen '{blobItem.Name}' eksisterer ikke.", ConsoleColor.Red);
                return String.Empty;
            }

            return await ProcessZipBlobAsync(blobItem, installationId);
        }

        private static async Task<string> ProcessZipBlobAsync(CloudBlockBlob blobItem, int installationId)
        {
            var fileName = GetFileName(blobItem.Name);

            if (!fileName.Contains("pd") || fileName.Contains("_BackUp"))
            {
                return "null"; // Ignores non power day files and backups
            }

            var Originaljson = await ReadBlobFileJson(fileName + ".json", fileName + ".zip", installationId.ToString());
            if (Originaljson == null)
                return "null";
            var productionDto = ProductionDto.FromJson(Originaljson);

            ExportToCSV(productionDto, blobItem.Name);

            bool didChange = false;

            Parallel.ForEach(productionDto.Inverters, inv =>
            {
                foreach (var production in inv.Production)
                {
                    if (production.Value >=  ApplicationVariables.MaxEnergyInJoules)
                    {
                        production.Value = 0;
                        didChange = true;
                    }
                }
            });

            if (didChange)
            {
                fileName = GetFileName(blobItem.Name);

                var updatedJson = ProductionDto.ToJson(productionDto);
                await WriteJson(json: Originaljson, fileName: $"{fileName}_BackUp", sn: installationId.ToString());// Backup
                await DeleteBlobFile(fileName, installationId); // Delete original
                await WriteJson(json: updatedJson, fileName: fileName, sn: installationId.ToString());    // Uploadedre
                return Originaljson;
            }
            else
            {
                return Originaljson;
            }

        }

        public static void Log(string title, ConsoleColor color = ConsoleColor.DarkMagenta)
        {
            var banner = FiggleFonts.Standard.Render(title);
            Console.ForegroundColor = color;
            Console.WriteLine(banner);
            Console.ResetColor();
        }

        public static void LogError(string title, ConsoleColor color = ConsoleColor.DarkMagenta)
        {
            var banner = FiggleFonts.Standard.Render(title);
            Console.ForegroundColor = color;
            Console.WriteLine(banner);
            Console.ResetColor();
        }

        public static void Message(string message)
        {
            //var banner = FiggleFonts.Alligator2.Render(message);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
