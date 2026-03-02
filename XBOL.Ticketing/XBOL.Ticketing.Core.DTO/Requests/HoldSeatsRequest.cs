namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class HoldSeatsRequest
    {
        public required long EventId { get; set; }
        public required List<string> Seats { get; set; }
    }
}
