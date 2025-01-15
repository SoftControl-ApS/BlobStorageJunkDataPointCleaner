using Microsoft.Extensions.Configuration;
using System.IO;

namespace SharedLibrary
{


    public class ApplicationVariables
    {
        static ApplicationVariables()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
        }

        public static IConfiguration Configuration { get; }

        #region Private Fields
        internal static double MaxEnergyInJoules { get; set; }
        public static string AzureBlobConnectionName { get; } = Configuration["AzureBlob:ConnectionName"];

        public static string AzureBlobConnectionKey { get; } = Configuration["AzureBlob:ConnectionKey"];

        public static string AzureBlobConnectionString { get; } =
            $"DefaultEndpointsProtocol=https;AccountName={AzureBlobConnectionName};" +
            $"AccountKey={AzureBlobConnectionKey};" +
            $"EndpointSuffix=core.windows.net";

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
