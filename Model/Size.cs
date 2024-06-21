using Newtonsoft.Json;

namespace ExcelTableConverter.Model
{
    public class Size
    {
        [JsonProperty("width")]
        public ulong Width { get; set; }

        [JsonProperty("height")]
        public ulong Height { get; set; }
    }
}
