using System.Text.Json.Serialization;

namespace XBOL.Ticketing.Core.Commons.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EventStatus
    {
        Draft,
        PendingReview,
        Approved,
        ChangesRequested,
        Published,
        Cancelled
    }
}
