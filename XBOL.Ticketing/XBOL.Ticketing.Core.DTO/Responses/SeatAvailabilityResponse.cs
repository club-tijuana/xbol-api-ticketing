namespace XBOL.Ticketing.Core.DTO.Responses
{
    public class SeatAvailabilityResponse
    {
        public IList<ZoneResponse> Zones { get; set; } = new List<ZoneResponse>();
        public IList<SeatResponse> SeatOverrides { get; set; } = new List<SeatResponse>();
    }
}
