using System.Text.Json.Serialization;

namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class EvoError
    {
        [JsonPropertyName("cause")]
        public string Cause { get; set; } = string.Empty;
        [JsonPropertyName("explanation")]
        public string Explanation { get; set; } = string.Empty;
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;
        [JsonPropertyName("supportCode")]
        public string SupportCode { get; set; } = string.Empty;
        [JsonPropertyName("validationType")]
        public string ValidationType { get; set; } = string.Empty;
    }
}
