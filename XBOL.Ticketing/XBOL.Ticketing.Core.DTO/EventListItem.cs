namespace XBOL.Ticketing.Core.DTO
{
    public class EventListItem
    {
        public long Id { get; set; }
        public DateTimeOffset DateTime { get; set; }
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public long VenueMapId { get; set; }
        public int AvailableSeats { get; set; }
        public int TotalSeats { get; set; }
    }
}
