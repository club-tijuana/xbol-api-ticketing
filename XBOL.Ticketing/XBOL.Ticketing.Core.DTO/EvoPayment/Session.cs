using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class Session
    {
        [JsonPropertyName("aes256Key")]
        public string Aes256Key { get; set; } = string.Empty;
        [JsonPropertyName("authenticationLimit")]
        public long AuthenticationLimit { get; set; }
        [JsonPropertyName("id")]
        [MinLength(31), MaxLength(35)]
        public required string Id { get; set; }
        [JsonPropertyName("updateStatus")]
        public required string UpdateStatus { get; set; }
        [JsonPropertyName("version")]
        [MinLength(10), MaxLength(10)]
        public required string Version { get; set; }
    }
}
