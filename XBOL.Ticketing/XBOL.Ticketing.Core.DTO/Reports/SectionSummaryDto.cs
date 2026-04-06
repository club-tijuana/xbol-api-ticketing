using System.Text.Json.Serialization;

namespace XBOL.Ticketing.Core.DTO.Reports;

/// <summary>
/// Represents the summary of seat availability and statuses for a specific section.
/// </summary>
public class SectionSummaryDto
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("byStatus")]
    public Dictionary<string, int> ByStatus { get; set; } = [];

    [JsonPropertyName("byAvailability")]
    public Dictionary<string, int> ByAvailability { get; set; } = [];

    [JsonPropertyName("byCategoryLabel")]
    public Dictionary<string, int> ByCategoryLabel { get; set; } = [];

    [JsonPropertyName("byCategoryKey")]
    public Dictionary<string, int> ByCategoryKey { get; set; } = [];

    [JsonPropertyName("byChannel")]
    public Dictionary<string, int> ByChannel { get; set; } = [];
}
