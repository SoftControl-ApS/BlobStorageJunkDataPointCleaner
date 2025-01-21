using Newtonsoft.Json;

namespace SharedLibrary.Models;

public partial class Inverter
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public int? Id { get; set; }

    [JsonProperty("production", NullValueHandling = NullValueHandling.Ignore)]
    public List<DataPoint> Production { get; set; }
}