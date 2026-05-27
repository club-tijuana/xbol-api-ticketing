using System.ComponentModel;
using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO
{
    /// <summary>
    /// An EventSchedule associated with a Bundle.
    /// </summary>
    public class BundleEventScheduleResponseDTO
    {
        public long BundleId { get; set; }
        public long EventScheduleId { get; set; }

        /// <summary>
        /// Sort position within the Bundle. Lower values appear first.
        /// </summary>
        public int? SortOrder { get; set; }

        public EventScheduleSummaryDTO EventSchedule { get; set; } = null!;
    }

    /// <summary>
    /// Read-only EventSchedule summary embedded in Bundle association responses.
    /// </summary>
    public class EventScheduleSummaryDTO
    {
        public long Id { get; set; }
        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset EndDateTime { get; set; }

        [Description("Seats.io external key. Null if not yet synced.")]
        public string? ExternalEventKey { get; set; }
        public GameCategory GameCategory { get; set; }
        public ScheduleStatus Status { get; set; }
    }
}
