using Newtonsoft.Json;

namespace XBOL.Ticketing.API.Models;

public class CursorPagedResponse<T>
{
    [JsonProperty("next_page_starts_after")]
    public long? NextPageStartsAfter { get; set; }

    [JsonProperty("items")]
    public List<T> Items { get; set; } = [];
}
