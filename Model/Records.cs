using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OptimizeBot.Model
{
    [JsonObject(MemberSerialization.OptOut, NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public record Line(double From, double To, double Fee);

    [JsonObject(MemberSerialization.OptOut, NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public record Utility(string Id, string CompanyName);

    [JsonObject(MemberSerialization.OptOut, NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public record ReceiptContext()
    {
        [JsonProperty(Required = Required.AllowNull)]
        public int ProviderId { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public int UtilityCount { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public string UtilityProviderId { get; set; } = default!;

        [JsonProperty(Required = Required.AllowNull)]
        public string TrxId { get; set; } = default!;
    }

    [JsonObject(MemberSerialization.OptOut, NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public record CashContext([JsonProperty(Required = Required.Always)] int ProviderId,
                              [JsonProperty(Required = Required.Always)] double MinAmount,
                              [JsonProperty(Required = Required.Always)] double MaxAmount)
    {
        [JsonProperty(Required = Required.AllowNull)]
        public double? Amount { get; set; }
    }

    [JsonObject(MemberSerialization.OptOut, NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public record DownloadedLink([JsonProperty(Required = Required.Always)] string Link);
}
