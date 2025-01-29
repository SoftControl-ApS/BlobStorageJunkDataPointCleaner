using Microsoft.Extensions.Configuration;
using System.IO;

namespace SharedLibrary
{
    public class ApplicationVariables
    {
        public static IConfiguration Configuration { get; }
        public static object locktotalFile { get; } = new();
        static ApplicationVariables()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();


            // Initialize static properties after Configuration is built
            //AzureBlobConnectionName = Configuration["AzureBlob:ConnectionName"];
            //AzureBlobConnectionKey = Configuration["AzureBlob:ConnectionKey"];

            AzureBlobConnectionName = "sundatatest";
            AzureBlobConnectionKey = "z1CzWXUvl3756GlrguOi/5Iwn7w+ILfAzlxJ/dOdz2UG+8w2vbKXT0rkBllvpCg0IDhAC6RmeEsL+AStzJa0Bw==";

            AzureBlobConnectionString =
                $"DefaultEndpointsProtocol=https;AccountName={AzureBlobConnectionName};" +
                $"AccountKey={AzureBlobConnectionKey};" +
                $"EndpointSuffix=core.windows.net";
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
