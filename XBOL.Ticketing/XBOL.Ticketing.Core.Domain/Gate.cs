namespace XBOL.Ticketing.Core.Model
{
    public class Gate
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;

        public long VenueMapId { get; set; }
        public VenueMap VenueMap { get; set; } = null!;

        public IList<TicketScanLog> TicketScanLogs { get; set; } = [];
    }
}