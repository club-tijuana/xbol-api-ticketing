namespace XBOL.Ticketing.Core.DTO
{
    public class EventListItem
    {
        public long Id { get; set; }
        public DateTimeOffset ScheduledStartDate { get; set; }
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public long VenueMapId { get; set; }
        public string VenueName { get; set; } = null!;
        public string ExternalEventKey { get; set; } = null!;
        public int AvailableSeats { get; set; }
        public int TotalSeats { get; set; }
    }
}
