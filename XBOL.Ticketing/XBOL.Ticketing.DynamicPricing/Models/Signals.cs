namespace XBOL.Ticketing.DynamicPricing.Models
{
    /// <summary>
    /// A signal is raw, external/internal or direclty data that can influence pricing decisions.
    /// </summary>
    public class Signals
    {
        // Venue related signals
        public string VenueCategory { get; set; } = null!;
        public decimal VenueLatitude { get; set; }
        public decimal VenueLongitude { get; set; }
        public int VenueCapacity { get; set; }

        // Event related signals
        public string EventCategory { get; set; } = null!;
        public DateTimeOffset EventDateTime { get; set; }
        public string EventGameCategory { get; set; } = null!;
    }
}
