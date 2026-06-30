namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public sealed class GatewayOptions
    {
        public const string SectionName = "MastercardGateway";
        public string GatewayBaseUrl { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "100";
        public string MerchantId { get; set; } = string.Empty;
        public string ApiPassword { get; set; } = string.Empty;
        public string Currency { get; set; } = "MXN";
        public string Amount { get; set; } = "10.00";
    }
}
