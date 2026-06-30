using XBOL.Ticketing.Core.DTO.Requests;

namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class InitiateCheckoutRequest
    {
        public long? EventScheduleId { get; init; }

        public long? BundleId { get; set; }
        public long? RelatedOrderId { get; set; }

        public required string HoldToken { get; init; }

        public required IReadOnlyList<CheckoutSeatRequest> Seats { get; init; }

        public required ClientInfoRequest ClientContact { get; init; }

        public required string ReturnUrl { get; init; }

        public string Currency { get; init; } = "MXN";
    }
}
