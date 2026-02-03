using System.ComponentModel;

namespace XBOL.Ticketing.Core.Commons.Enums
{
    public enum FeelingOfTheMarket
    {
        [Description("Conservative")]
        Conservative,

        [Description("Neutral")]
        Neutral,

        [Description("Optimist")]
        Optimist,

        [Description("Aggressive")]
        Aggressive,
    }
}