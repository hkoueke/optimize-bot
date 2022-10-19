using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptimizeBot.Model
{
    public class ProviderConfig
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ProviderId { get; set; }
        public Provider Provider { get; set; } = default!;
        public string? ReceiptApiUrl { get; set; }
        public string? ReceiptApiHost { get; init; }
        public string? ReceiptApiReferer { get; init; }

        public string BaseServiceUssd { get; set; } = default!;
        public int? TrxIdLength { get; init; } 
    }
}
