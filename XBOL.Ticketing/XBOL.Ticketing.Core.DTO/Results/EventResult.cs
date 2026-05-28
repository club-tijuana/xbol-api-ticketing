using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO.Results
{
    public class EventResult
    {
        public long Id { get; set; }
        public long? VenueMapId { get; set; }
        public string Name { get; set; } = null!;
        public string? Subtitle { get; set; }
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public List<EventCategoryResult> Categories { get; set; } = [];
        public EventStatus Status { get; set; }
    }
}
