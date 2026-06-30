using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class BookingRequest
    {
        public List<BookingSeatRequest> Seats { get; set; } = [];
        public string HoldToken { get; set; } = "";
        public long EventScheduleId { get; set; }
        public required ItemType TicketType { get; set; }
        public required ClientInfoRequest ClientContact { get; set; }
        public required PaymentInfoRequest PaymentInfoRequest { get; set; }
        public required ChangeInfoRequest ChangeInfoRequest { get; set; }
        public PaymentLinkRequest? PaymentLinkRequest { get; set; }
        public string? Localizer { get; set; }
        public Guid? UserProfileId { get; set; }
        public bool IsPaymentLink { get; set; }

        public string SessionId { get; set; } = string.Empty;
        public Guid OrderRefId { get; set; }
        public Guid TransactionRefId { get; set; }
    }
}
