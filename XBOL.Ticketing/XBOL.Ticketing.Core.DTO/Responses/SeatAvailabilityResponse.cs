namespace XBOL.Ticketing.Core.DTO.Responses
{
    public class SeatAvailabilityResponse
    {
        public IList<SectionResponse> Sections { get; set; } = new List<SectionResponse>();
        public IList<SeatResponse> SeatOverrides { get; set; } = new List<SeatResponse>();
    }
}
