namespace XBOL.Ticketing.Core.DTO
{
    public class BookingRequest
    {
        public required List<string> Seats { get; set; }
        public required string HoldToken { get; set; }
        public required string EventId { get; set; }
    }
}
