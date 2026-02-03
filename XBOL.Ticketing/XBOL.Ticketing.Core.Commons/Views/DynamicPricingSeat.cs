using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Commons.Views
{
    public class DynamicPricingSeat
    {
        public long SeatId { get; set; }
        public string SeatZone { get; set; } = null!;
        public string SeatSection { get; set; } = null!;
        public string SeatRow { get; set; } = null!;
        public string SeatNumber { get; set; } = null!;
        public SeatType SeatType { get; set; }
        public decimal? SectionBasePrice { get; set; }
        public bool IsSold { get; set; }
    }
}