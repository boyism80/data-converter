using Newtonsoft.Json;

namespace ExcelTableConverter.Model
{
    public class Area
    {
        [JsonProperty("left")] public ulong Left { get; set; }
        [JsonProperty("top")] public ulong Top { get; set; }
        [JsonProperty("right")] public ulong Right { get; set; }
        [JsonProperty("bottom")] public ulong Bottom { get; set; }
    }
}
