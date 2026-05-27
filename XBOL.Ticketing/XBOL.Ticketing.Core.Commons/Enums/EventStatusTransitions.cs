namespace XBOL.Ticketing.Core.Commons.Enums
{
    public static class EventStatusTransitions
    {
        private static readonly Dictionary<EventStatus, HashSet<EventStatus>> Transitions = new()
        {
            [EventStatus.Draft] = [EventStatus.PendingReview, EventStatus.Cancelled],
            [EventStatus.PendingReview] = [EventStatus.Draft, EventStatus.Approved, EventStatus.ChangesRequested, EventStatus.Cancelled],
            [EventStatus.ChangesRequested] = [EventStatus.Draft, EventStatus.PendingReview, EventStatus.Cancelled],
            [EventStatus.Approved] = [EventStatus.Published, EventStatus.Cancelled],
            [EventStatus.Published] = [EventStatus.Cancelled],
            [EventStatus.Cancelled] = []
        };

        public static bool IsValidTransition(EventStatus from, EventStatus to)
        {
            return Transitions.TryGetValue(from, out var targets) && targets.Contains(to);
        }

        public static void ValidateTransition(EventStatus from, EventStatus to)
        {
            if (!IsValidTransition(from, to))
            {
                throw new InvalidOperationException(
                    $"Invalid status transition from '{from}' to '{to}'.");
            }
        }
    }
}
