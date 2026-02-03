namespace XBOL.Ticketing.Core.DTO
{
    public class VenueMapListItem
    {
        public long Id { get; set; }
        public long VenueId { get; set; }
        public string Name { get; set; } = null!;
        public string ExternalMapKey { get; set; } = null!;
    }
}
