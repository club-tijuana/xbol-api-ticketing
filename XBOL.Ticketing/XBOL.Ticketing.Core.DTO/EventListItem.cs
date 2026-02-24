namespace XBOL.Ticketing.Core.DTO
{
    public class EventListItem
    {
        public required long Id { get; set; }
        public required DateTimeOffset ScheduledStartDate { get; set; }
        public required string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public required long VenueMapId { get; set; }
        public string VenueName { get; set; } = null!;
        public string ExternalEventKey { get; set; } = null!;
        public required int AvailableSeats { get; set; }
        public required int TotalSeats { get; set; }
    }
}
