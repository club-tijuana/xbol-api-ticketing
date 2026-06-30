namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class ConfirmCheckoutRequest
    {
        public long LocalOrderId { get; init; }
        public required string OrderRefId { get; init; }
        public string? ResultIndicator { get; init; }
    }
}
