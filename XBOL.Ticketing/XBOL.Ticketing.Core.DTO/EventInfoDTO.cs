using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO
{
    public class EventInfoDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Subtitle { get; set; }
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public string? BannerImageUrl { get; set; }
        public List<Results.EventCategoryResult> Categories { get; set; } = [];
        public long? VenueId { get; set; }
        public long? VenueMapId { get; set; }
        public string? VenueName { get; set; }
        public AgeRestriction? AgeRestriction { get; set; }
        public string? SecurityPolicies { get; set; }
        public string? AdditionalComments { get; set; }
        public EventStatus Status { get; set; }
        public IList<EventScheduleDTO> Schedules { get; set; } = [];
    }
}
