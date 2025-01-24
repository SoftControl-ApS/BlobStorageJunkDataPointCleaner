using Newtonsoft.Json;
namespace SharedLibrary.Models;
public partial class DataPoint
{
    [JsonProperty("timeStamp", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? TimeStamp { get; set; }

    [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(ZeroIfNullOrNaNConverter))]
    public double? Value { get; set; }

    [JsonProperty("quality", NullValueHandling = NullValueHandling.Ignore)]
    public long? Quality { get; set; }
}

public class ZeroIfNullOrNaNConverter : JsonConverter<double?>
{
    public override void WriteJson(JsonWriter writer, double? value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }

    public override double? ReadJson(JsonReader reader, Type objectType, double? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null || reader.TokenType == JsonToken.Undefined)
        {
            return 0;
        }

        if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
        {
            return Convert.ToDouble(reader.Value);
        }

        if (reader.TokenType == JsonToken.String)
        {
            var stringValue = reader.Value.ToString();
            if (double.TryParse(stringValue, out double result))
            {
                return result;
            }
        }

        return 0;
    }
}