namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class HoldSeasonSeatsRequest
    {
        public required long SeasonId { get; set; }
        public required List<string> Seats { get; set; }
    }
}
