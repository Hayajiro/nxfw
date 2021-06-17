using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace nxfw.WebClient
{
    public class CdnVersionCheckResult
    {
        [JsonProperty("timestamp")] public long Timestamp { get; set; }

        [JsonProperty("system_update_metas")] public SystemUpdateMeta[] SystemUpdateMetas { get; set; }

        public static CdnVersionCheckResult FromJson(string json) =>
            JsonConvert.DeserializeObject<CdnVersionCheckResult>(json,
                new JsonSerializerSettings
                {
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                    DateParseHandling = DateParseHandling.None,
                    Converters =
                    {
                        new IsoDateTimeConverter {DateTimeStyles = DateTimeStyles.AssumeUniversal}
                    },
                }
            );
    }

    public class SystemUpdateMeta
    {
        [JsonProperty("title_id")]
        public string TitleId { get; set; }

        [JsonProperty("title_version")]
        public uint TitleVersion { get; set; }
    }
}