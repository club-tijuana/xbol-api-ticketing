using System.Text.Json.Serialization;

namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class UpdateSessionRequest
    {
        [JsonPropertyName("order")]
        public EvoOrder? Order { get; set; }
    }
}
