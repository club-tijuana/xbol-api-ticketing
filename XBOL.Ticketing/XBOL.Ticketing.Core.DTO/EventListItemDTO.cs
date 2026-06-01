namespace XBOL.Ticketing.Core.DTO
{
    public class EventListItemDTO
    {
        public required long Id { get; set; }
        public required DateTimeOffset ScheduledStartDate { get; set; }
        public required string Name { get; set; }
        public List<Results.EventCategoryResult> Categories { get; set; } = [];
        public long? VenueMapId { get; set; }
        public string? VenueName { get; set; }
        public string? ExternalEventKey { get; set; }
        public required int AvailableSeats { get; set; }
        public required int TotalSeats { get; set; }
        public string? PosterImageUrl { get; set; }
        public string? BannerImageUrl { get; set; }
        public bool IsSeason { get; set; }
    }
}
