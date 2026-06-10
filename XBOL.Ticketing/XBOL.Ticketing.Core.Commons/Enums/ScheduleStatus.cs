using System.Text.Json.Serialization;

namespace XBOL.Ticketing.Core.Commons.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ScheduleStatus
    {
        Draft,
        OnSale,
        Closed,
        Completed
    }
}
