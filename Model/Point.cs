using Newtonsoft.Json;

namespace ExcelTableConverter.Model
{
    public class Point
    {
        [JsonProperty("x")]
        public ulong X { get; set; }

        [JsonProperty("y")]
        public ulong Y { get; set; }
    }
}
