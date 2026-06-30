namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class InitiateCheckoutResponse
    {
        public long LocalOrderId { get; init; }

        public required string SessionId { get; init; }
        public required string SuccessIndicator { get; init; }
        public required string OrderRefId { get; init; }
        public required string Amount { get; init; }
        public required string Currency { get; init; }
        public required string MerchantId { get; init; }
        public required string ApiVersion { get; init; }
        public required string GatewayBaseUrl { get; init; }
    }
}
