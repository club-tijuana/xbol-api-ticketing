using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Results;

namespace XBOL.Ticketing.Core.DTO
{
    public class BundleScheduleItemDTO
    {
        public required long EventId { get; set; }
        public required long EventScheduleId { get; set; }
        public required DateTimeOffset ScheduledStartDate { get; set; }
        public DateTimeOffset? ScheduledEndDate { get; set; }
        public required string Name { get; set; }
        public List<EventCategoryResult> Categories { get; set; } = [];
        public long? VenueMapId { get; set; }
        public string? VenueName { get; set; }
        public string? ExternalEventKey { get; set; }
        public ScheduleStatus Status { get; set; }
        public required int AvailableSeats { get; set; }
        public required int TotalSeats { get; set; }
    }
}
