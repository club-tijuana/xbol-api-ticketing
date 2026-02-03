using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Commons.Views
{
    public class DynamicPricingEvent
    {
        public long EventScheduleId { get; set; }
        public VenueCategory VenueCategory { get; set; }
        public decimal VenueLatitude { get; set; }
        public decimal VenueLongitude { get; set; }

        public int? VenueCapacity { get; set; }

        public EventCategory EventCategory { get; set; }
        public DateTimeOffset EventDateTime { get; set; }
        public DateTimeOffset EventPublishedDate { get; set; }
        public GameCategory EventGameCategory { get; set; }

        public ProfitabilityType EventProfitability { get; set; }
        public FeelingOfTheMarket FeelingOfTheMarket { get; set; }

        public IList<DynamicPricingSeat> Seats { get; set; } = [];
    }
}