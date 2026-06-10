namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class SyncChartRequest
    {
        public long VenueMapId { get; set; }
        public Guid UserId { get; set; } = Guid.Empty;
    }
}
