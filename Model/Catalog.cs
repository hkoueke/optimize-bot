namespace OptimizeBot.Model
{
    public class Catalog
    {
        public int ProviderId { get; set; }
        public Provider Provider { get; set; } = default!;
        public int ServiceId { get; set; }
        public Service Service { get; set; } = default!;
        public int? PricingId { get; set; }
        public Pricing Pricing { get; set; } = default!;
    }
}