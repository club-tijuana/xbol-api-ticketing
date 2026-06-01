using System.ComponentModel;

namespace XBOL.Ticketing.Core.DTO.Requests
{
    /// <summary>
    /// Add one or more EventSchedules to a Bundle.
    /// </summary>
    public class BundleEventScheduleAddRequest
    {
        public required List<BundleEventScheduleItem> Items { get; set; }
    }

    /// <summary>
    /// A single EventSchedule to associate with a Bundle.
    /// </summary>
    public class BundleEventScheduleItem
    {
        public required long EventScheduleId { get; set; }

        [Description("Sort position within the Bundle. Lower values appear first. Null means no explicit ordering.")]
        public int? SortOrder { get; set; }
    }
}
