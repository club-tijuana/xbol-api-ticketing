namespace XBOL.Ticketing.Core.Commons.Views
{
    public class DynamicPricingRule
    {
        public long Id { get; set; }
        public string Code { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Expression { get; set; } = null!;
    }
}