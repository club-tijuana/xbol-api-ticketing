namespace XBOL.Ticketing.Core.DTO
{
    public class VenueMapListItem
    {
        public required long Id { get; set; }
        public required long VenueId { get; set; }
        public string Name { get; set; } = null!;
        public string ExternalMapKey { get; set; } = null!;
    }
}
