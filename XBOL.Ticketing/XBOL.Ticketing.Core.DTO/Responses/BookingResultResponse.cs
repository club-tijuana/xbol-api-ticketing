namespace XBOL.Ticketing.Core.DTO.Responses
{
    public class BookingResultResponse
    {
        public long OrderId { get; set; }
        public string Reference { get; set; } = null!;
        public IEnumerable<string> BookedSeatKeys { get; set; } = [];
        public IEnumerable<long> TicketIds { get; set; } = [];
        public IEnumerable<long> BundlePassIds { get; set; } = [];
        public long ClientId { get; set; }
        public decimal Total { get; set; }
    }
}
