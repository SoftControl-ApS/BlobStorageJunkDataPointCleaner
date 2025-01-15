using SharedLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SharedLibrary.SunSys
{
    public partial class SunSystemAPI
    {
        private HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri("http://sundataapi.azurewebsites.net"),
            Timeout = new TimeSpan(100000000000),

        };

        public SunSystemAPI()
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<InverterAPI>> GetAllInvertersAsync(int installationId)
        {
            try
            {
                var response = await client.GetAsync("/api/Inverter/GetAllInverter?Instid=" + installationId);
                response.EnsureSuccessStatusCode();

                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };

                var jsonString = await response.Content.ReadAsStringAsync();
                var inverters = JsonConvert.DeserializeObject<List<InverterAPI>>(jsonString, settings);

                return inverters;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching inverters: {ex.Message}");
                return null;
            }
        }



        public async Task<List<InverterModeAPI>> GetInverterModelList()
        {
            HttpResponseMessage response = client.GetAsync("/api/Inverter/GetInverterBrand").Result;  // Blocking call!
            response.EnsureSuccessStatusCode();

            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var jsonString = await response.Content.ReadAsStringAsync();
            var invModels = JsonConvert.DeserializeObject<List<InverterModeAPI>>(jsonString, settings);

            return invModels;
        }
    }



    public class InverterModeAPI
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string power { get; set; }
        public InverterBrand InverterBrand { get; set; }
    }
    public class InverterBrand
    {
        public int id { get; set; }
        public string name { get; set; }
    }



    public class InverterAPI
    {
        [JsonProperty("Id", NullValueHandling = NullValueHandling.Ignore)]
        public long? InverterId { get; set; }

        [JsonProperty("Name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("DegradeFactor", NullValueHandling = NullValueHandling.Ignore)]
        public long? DegradeFactor { get; set; }

        [JsonProperty("DegradeAlgorithm", NullValueHandling = NullValueHandling.Ignore)]
        public long? DegradeAlgorithm { get; set; }

        [JsonProperty("CfgPowerLimit")]
        public object CfgPowerLimit { get; set; }

        [JsonProperty("SerialNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string SerialNumber { get; set; }

        [JsonProperty("DegradeStartYear", NullValueHandling = NullValueHandling.Ignore)]
        public long? DegradeStartYear { get; set; }

        [JsonProperty("OperatingModeId", NullValueHandling = NullValueHandling.Ignore)]
        public long? OperatingModeId { get; set; }

        [JsonProperty("CnfTemperatureAvailable", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CnfTemperatureAvailable { get; set; }

        [JsonProperty("Installation", NullValueHandling = NullValueHandling.Ignore)]
        public Installation Installation { get; set; }

        [JsonProperty("InverterModel", NullValueHandling = NullValueHandling.Ignore)]
        public InverterModel InverterModel { get; set; }
    }

    public partial class Installation
    {

        [JsonProperty("Id", NullValueHandling = NullValueHandling.Ignore)]
        public long? InstallationId { get; set; }

        [JsonProperty("Name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("Description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }
    }

    public partial class InverterModel
    {

        [JsonProperty("Id", NullValueHandling = NullValueHandling.Ignore)]
        public long? InverterModelId { get; set; }

        [JsonProperty("Name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }
}

