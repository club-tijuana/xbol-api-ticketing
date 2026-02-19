namespace XBOL.Ticketing.Core.DTO
{
    public class BookingRequest
    {
        public required List<string> Seats { get; set; }
        public required string HoldToken { get; set; }
        public required string EventId { get; set; }
        public string? TicketType { get; set; }
        public required EventClientContactRequest ClientContact { get; set; }
        public required PaymentInfoRequest PaymentInfoRequest { get; set; }
    }
}
