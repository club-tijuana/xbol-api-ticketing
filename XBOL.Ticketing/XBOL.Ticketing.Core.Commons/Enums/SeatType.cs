using System.ComponentModel;

namespace XBOL.Ticketing.Core.Commons.Enums
{
    public enum SeatType
    {
        [Description("Stadium")]
        Standard,

        [Description("Accessible")]
        Accessible,

        [Description("Vip")]
        Vip
    }
}