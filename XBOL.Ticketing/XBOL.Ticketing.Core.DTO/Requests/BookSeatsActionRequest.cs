using System.ComponentModel.DataAnnotations;
using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class BookSeatsActionRequest
    {
        public required string EventKey { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one seat is required.")]
        public required List<BookingSeatRequest> Seats { get; set; } = [];

        public string HoldToken { get; set; } = "";
        public long? BundleId { get; set; }
        public long EventScheduleId { get; set; }
        public Guid? UserProfileId { get; set; }
        public required ItemType TicketType { get; set; }
        public required ClientInfoRequest ClientContact { get; set; }
        public required PaymentInfoRequest PaymentInfoRequest { get; set; }
        public required ChangeInfoRequest? ChangeInfoRequest { get; set; }
        public string? Localizer { get; set; }
        public long? ReferenceOrderId { get; set; }
        public bool OverrideSaleWindow { get; set; }

        public bool IsPaymentLink { get; set; }
        public PaymentLinkRequest? PaymentLinkRequest { get; set; }
        public Guid TransactionRefId { get; set; }
    }
}
