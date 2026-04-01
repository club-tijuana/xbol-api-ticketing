namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class UpdateSeatExtraDataRequest
    {
        public required string EventKey { get; set; }
        public required List<string> SeatKeys { get; set; }
        public required Dictionary<string, object> ExtraData { get; set; }
    }
}
