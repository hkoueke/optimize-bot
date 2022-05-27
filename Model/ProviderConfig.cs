using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptimizeBot.Model
{
    public class ProviderConfig
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ProviderId { get; set; }

        public Provider Provider { get; set; }

#nullable enable
        public string? ReceiptApiUrl { get; set; }

        public string? ReceiptApiHost { get; init; }

        public string? ReceiptApiReferer { get; init; }

#nullable disable
        public string BaseServiceUssd { get; set; }

        public int? TrxIdLength { get; init; } 
    }
}
