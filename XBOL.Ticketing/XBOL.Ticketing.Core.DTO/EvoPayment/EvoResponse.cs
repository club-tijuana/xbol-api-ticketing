using System.Text.Json.Serialization;

namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class EvoResponse
    {
        [JsonPropertyName("gatewayCode")]
        public string GatewayCode { get; set; } = string.Empty;
    }
}
