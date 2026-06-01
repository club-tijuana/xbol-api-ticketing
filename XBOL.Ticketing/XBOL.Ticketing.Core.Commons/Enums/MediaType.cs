using System.Text.Json.Serialization;

namespace XBOL.Ticketing.Core.Commons.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MediaType
    {
        Banner,
        Gallery,
        Logo,
        GeneralView,
        Facade,
        Sponsor,
    }
}
