using System.ComponentModel;

namespace XBOL.Ticketing.Core.Commons.Enums
{
    public enum ProfitabilityType
    {
        [Description("Low")]
        Low,

        [Description("Regular")]
        Regular,

        [Description("High")]
        High,

        [Description("Unique")]
        Unique,

        [Description("Premium")]
        Premium
    }
}