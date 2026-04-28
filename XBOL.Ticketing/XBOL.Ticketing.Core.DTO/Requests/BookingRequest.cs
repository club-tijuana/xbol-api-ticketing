using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class BookingRequest
    {
        public Dictionary<string, decimal> Seats { get; set; } = [];
        public string HoldToken { get; set; } = "";
        public long EventScheduleId { get; set; }
        public required ItemType TicketType { get; set; }
        public required ClientInfoRequest ClientContact { get; set; }
        public required PaymentInfoRequest PaymentInfoRequest { get; set; }
        public string? Localizer { get; set; }
    }
}
