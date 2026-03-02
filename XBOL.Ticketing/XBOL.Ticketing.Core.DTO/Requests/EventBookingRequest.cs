namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class EventBookingRequest : BookingRequest
    {
        public required string EventKey { get; set; } = "";
    }
}
