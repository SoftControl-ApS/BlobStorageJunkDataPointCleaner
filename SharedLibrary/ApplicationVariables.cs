using Microsoft.Extensions.Configuration;
using System.IO;

namespace SharedLibrary
{
    public class ApplicationVariables
    {
        public static IConfiguration Configuration { get; }
        public static object locktotalFile { get; } = new();
        public static List<Failed> FailedFiles = new();

        private static string _logFilePath = "log.txt";
        public static string LogFilePath { get => _logFilePath; }

        static ApplicationVariables()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();


            AzureBlobConnectionName = "sundatatest";
            AzureBlobConnectionKey = "z1CzWXUvl3756GlrguOi/5Iwn7w+ILfAzlxJ/dOdz2UG+8w2vbKXT0rkBllvpCg0IDhAC6RmeEsL+AStzJa0Bw==";

            ////AzureBlobConnectionName = "sundata";
            ////AzureBlobConnectionKey = "/y8BUVnCBJfKsvgwLZkl3mMaZ3OB/15QmMP/J0TJezps0QloO0CR/dJS16MjK/t1dO1GEFQT7FTVXhhXIE3wrQ==";

            AzureBlobConnectionString =
                $"DefaultEndpointsProtocol=https;AccountName={AzureBlobConnectionName};" +
                $"AccountKey={AzureBlobConnectionKey};" +
                $"EndpointSuffix=core.windows.net";

            // AzureBlobConnectionString = "DefaultEndpointsProtocol=https;AccountName=sundata;AccountKey=/y8BUVnCBJfKsvgwLZkl3mMaZ3OB/15QmMP/J0TJezps0QloO0CR/dJS16MjK/t1dO1GEFQT7FTVXhhXIE3wrQ==;EndpointSuffix=core.windows.net";

            // _logFilePath = Configuration["LogFileName"];
            // string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            // _logFilePath = Path.Combine(logDirectory, $"log-{timestamp}.txt");


            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string logDirectory = Path.Combine(desktopPath, "logs");

            // Check if the directory exists, if not, create it
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            _logFilePath = Path.Combine(logDirectory, $"log-{timestamp}.txt");

            // Check if the file exists, if not, create it
            if (!File.Exists(_logFilePath))
            {
                using (File.Create(_logFilePath)) { }
            }

        }
        public class Failed
        {
            public string Name { get; set; }
            public string Description { get; set; }

            public Failed(string name, string desc)
            {
                Name = name;
                Description = desc;
            }

        }


        #region Private Fields
        internal static double MaxEnergyInJoules { get; set; }
        public static string AzureBlobConnectionName { get; private set; }
        public static string AzureBlobConnectionKey { get; private set; }
        public static string AzureBlobConnectionString { get; private set; }
        #endregion

        public static double SetMaxEnergyInJoule(double value)
        {
            ApplicationVariables.MaxEnergyInJoules = value;
            return ApplicationVariables.MaxEnergyInJoules;
        }

        public static double GetMaxEnergyInJoule()
        {
            return ApplicationVariables.MaxEnergyInJoules;
        }
    }
}
