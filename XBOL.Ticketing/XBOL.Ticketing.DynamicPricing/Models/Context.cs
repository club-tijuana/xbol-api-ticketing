namespace XBOL.Ticketing.DynamicPricing.Models
{
    /// <summary>
    /// The Context is the Signals, Features and Rules that influence dynamic pricing calculations.
    /// </summary>
    public class Context
    {
        public required Signals Signals { get; set; }
        public required Features Features { get; set; }
        public required IList<Rule> Rules { get; set; } = [];
        public required IList<Rule> RulesTrace { get; set; } = [];
        public required IList<Rule> RulesToApply { get; set; } = [];
        public required ItemLists Catalogs { get; set; }
    }
}