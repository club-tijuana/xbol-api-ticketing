
namespace XBOL.Ticketing.Core.DTO.Requests
{
    /// <summary>
    /// Remove EventSchedules from a Bundle. Non-associated IDs are silently skipped.
    /// </summary>
    public class BundleEventScheduleRemoveRequest
    {
        public required List<long> EventScheduleIds { get; set; }
    }
}
