using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class EventMedia
    {
        public long Id { get; set; }

        public long EventId { get; set; }
        public Event Event { get; set; } = null!;

        public string MediaType { get; set; } = null!;
        public string Url { get; set; } = null!;
        public int SortOrder { get; set; }
    }
}