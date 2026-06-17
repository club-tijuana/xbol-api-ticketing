namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class RetrieveOrderResponse
    {
        public required string OrderRefId { get; init; }
        public required string Result { get; init; }
        public string? Status { get; init; }
        public string? GatewayCode { get; init; }

        public decimal? TotalCapturedAmount { get; init; }
        public decimal? TotalAuthorizedAmount { get; init; }
        public string? Currency { get; init; }
        public string? CardNumberMasked { get; init; }
        public string? CardBrand { get; init; }
    }
}
