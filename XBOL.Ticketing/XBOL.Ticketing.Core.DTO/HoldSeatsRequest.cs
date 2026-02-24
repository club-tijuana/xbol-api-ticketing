namespace XBOL.Ticketing.Core.DTO
{
    public class HoldSeatsRequest
    {
        public required long EventId { get; set; }
        public required List<string> Seats { get; set; }
    }
}
