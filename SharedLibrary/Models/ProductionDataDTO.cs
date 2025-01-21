using Newtonsoft.Json;
using SharedLibrary.Models;
using static SharedLibrary.util.Util;
namespace SharedLibrary.Models;

public partial class ProductionDto
{
    [JsonProperty("timeType", NullValueHandling = NullValueHandling.Ignore)]
    public long? TimeType { get; set; }

    [JsonProperty("timeStamp", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? TimeStamp { get; set; }

    [JsonProperty("inverters", NullValueHandling = NullValueHandling.Ignore)]
    public List<Inverter> Inverters { get; set; }
}
