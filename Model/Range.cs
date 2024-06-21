using Newtonsoft.Json;

namespace ExcelTableConverter.Model
{
    public class Range
    {
        [JsonProperty("min")]
        public ulong Min { get; set; }

        [JsonProperty("max")]
        public ulong Max { get; set; }
    }
}
