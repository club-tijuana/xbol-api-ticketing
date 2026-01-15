namespace XBOL.Ticketing.DynamicPricing.Models
{
    /// <summary>
    /// A Rule is a logical condition that can be evaluated against the current Context to produce adjusments to calculate the dynamic price.
    /// </summary>
    public class Rule
    {
        public long Id { get; set; }
        public string Code { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Expression { get; set; } = null!;
        public long Version { get; set; }
    }
}
