namespace XBOL.Ticketing.DynamicPricing.Models
{
    public class DynamicPricingResult
    {
        public IList<PricedSeat> PricedSeats { get; set; } = new List<PricedSeat>();
        public DateTimeOffset CalculatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class PricedSeat
    {
        public long SeatId { get; set; }
        public string SeatCode { get; set; } = string.Empty;
        public decimal BasePrice { get; set; } = 0m;
        public decimal FinalPrice { get; set; } = 0m;
        public IList<AppliedRule> AppliedRules { get; set; } = new List<AppliedRule>();
    }

    public class AppliedRule
    {
        public string RuleCode { get; set; } = string.Empty;
        public decimal PriceAdjustment { get; set; }
        public string Expression { get; set; } = string.Empty;
        public long Version { get; set; }
    }
}
