using System.Text.Json.Serialization;

namespace XBOL.Ticketing.Core.Commons.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EventCatalogItemType
    {
        Event,
        Bundle
    }
}
