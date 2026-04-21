using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class BookSeatsActionRequest
    {
        public required string EventKey { get; set; }
        public required Dictionary<string, decimal> Seats { get; set; }
        public string HoldToken { get; set; } = "";
        public long EventScheduleId { get; set; }
        public required ItemType TicketType { get; set; }
        public required ClientInfoRequest ClientContact { get; set; }
        public required PaymentInfoRequest PaymentInfoRequest { get; set; }
        public string? Localizer { get; set; }
        public long? ReferenceOrderId { get; set; }
    }
}
