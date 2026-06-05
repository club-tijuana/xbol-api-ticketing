namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class HoldSeatsActionRequest
    {
        public long? BundleId { get; set; }
        public required string EventKey { get; set; }
        public required List<string> Seats { get; set; }
    }
}
