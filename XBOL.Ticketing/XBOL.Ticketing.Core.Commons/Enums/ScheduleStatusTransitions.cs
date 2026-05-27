namespace XBOL.Ticketing.Core.Commons.Enums
{
    public static class ScheduleStatusTransitions
    {
        private static readonly Dictionary<ScheduleStatus, HashSet<ScheduleStatus>> Transitions = new()
        {
            [ScheduleStatus.Draft] = [ScheduleStatus.OnSale, ScheduleStatus.Closed],
            [ScheduleStatus.OnSale] = [ScheduleStatus.Closed, ScheduleStatus.Completed],
            [ScheduleStatus.Closed] = [ScheduleStatus.Completed],
            [ScheduleStatus.Completed] = []
        };

        public static bool IsValidTransition(ScheduleStatus from, ScheduleStatus to)
        {
            return Transitions.TryGetValue(from, out var targets) && targets.Contains(to);
        }

        public static void ValidateTransition(ScheduleStatus from, ScheduleStatus to)
        {
            if (!IsValidTransition(from, to))
            {
                throw new InvalidOperationException(
                    $"Invalid schedule status transition from '{from}' to '{to}'.");
            }
        }

        public static void ValidateCanGoOnSale(EventStatus parentEventStatus)
        {
            if (parentEventStatus is EventStatus.Draft or EventStatus.Cancelled)
            {
                throw new InvalidOperationException(
                    $"Cannot set schedule to OnSale while parent event status is {parentEventStatus}. Event must be Approved or Published.");
            }
        }
    }
}
