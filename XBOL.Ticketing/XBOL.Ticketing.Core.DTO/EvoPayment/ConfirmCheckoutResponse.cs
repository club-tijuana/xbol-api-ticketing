namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class ConfirmCheckoutResponse
    {
        public long OrderId { get; init; }

        public required string OrderStatus { get; init; }

        public required string PaymentStatus { get; init; }

        public int TicketsIssued { get; init; }

        public string? Reference { get; init; }
    }
}
