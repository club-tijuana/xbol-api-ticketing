using System.Text.Json.Serialization;

namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class SessionResponse
    {
        [JsonPropertyName("merchant")]
        public string Merchant { get; set; } = string.Empty;
        [JsonPropertyName("result")]
        public string? Result { get; set; }
        [JsonPropertyName("session")]
        public Session? Session { get; set; }
        [JsonPropertyName("error")]
        public EvoError? Error { get; set; }
        [JsonPropertyName("order")]
        public EvoOrder? Order { get; set; }
        [JsonPropertyName("response")]
        public EvoResponse? EvoResponse { get; set; }

        public string OrderRefId { get; set; } = string.Empty;
        public string TransactionRefId { get; set; } = string.Empty;
    }
}
