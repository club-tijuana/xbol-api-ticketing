namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class SeasonBookingRequest : BookingRequest
    {
        public string SeasonKey { get; set; } = "";
        public long? ReferenceOrderId { get; set; }
    }
}
