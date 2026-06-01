using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Data.Abstractions;

namespace XBOL.Ticketing.Services.Bundle
{
    internal static class BundleEventScheduleValidator
    {
        private static readonly HashSet<EventStatus> EditableStatuses =
        [
            EventStatus.Draft,
            EventStatus.PendingReview,
            EventStatus.Approved
        ];

        private static readonly HashSet<EventStatus> SelectableEventStatuses =
        [
            EventStatus.PendingReview,
            EventStatus.Approved,
            EventStatus.Published
        ];

        private static readonly HashSet<ScheduleStatus> SelectableScheduleStatuses =
        [
            ScheduleStatus.Draft,
            ScheduleStatus.OnSale
        ];

        public static void ValidateBundleStatus(
            Core.Model.Bundle bundle,
            bool allowPublishedSeasonPassAdd = false)
        {
            if (EditableStatuses.Contains(bundle.Status))
            {
                return;
            }

            if (allowPublishedSeasonPassAdd &&
                bundle.Status == EventStatus.Published &&
                bundle.BundleType == BundleType.SeasonPass)
            {
                return;
            }

            if (bundle.Status == EventStatus.Published &&
                bundle.BundleType != BundleType.SeasonPass)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Cannot modify schedules for bundle in status '{bundle.Status}'. " +
                $"Allowed statuses: {string.Join(", ", EditableStatuses)}.");
        }

        public static async Task<Core.Model.EventSchedule> ValidateAdditionAsync(
            Core.Model.Bundle bundle,
            long eventScheduleId,
            IBundleEventScheduleRepository bundleEventScheduleRepository,
            IEventScheduleRepository eventScheduleRepository)
        {
            var eventSchedule = await eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(eventScheduleId)
                ?? throw new KeyNotFoundException($"EventSchedule {eventScheduleId} not found.");

            if (bundle.Id != 0 && await bundleEventScheduleRepository.ExistsAsync(bundle.Id, eventScheduleId))
            {
                throw new InvalidOperationException(
                    $"EventSchedule {eventScheduleId} is already in Bundle {bundle.Id}.");
            }

            if (eventSchedule.Event is Core.Model.Bundle)
            {
                throw new InvalidOperationException(
                    $"EventSchedule {eventScheduleId} must belong to an Event, not a Bundle.");
            }

            if (eventSchedule.Event.VenueMapId != bundle.VenueMapId)
            {
                throw new InvalidOperationException(
                    $"EventSchedule {eventScheduleId} belongs to a different venue map.");
            }

            ValidateSelectableSchedule(bundle, eventSchedule);

            if (bundle.BundleType != BundleType.SeasonPass)
            {
                return eventSchedule;
            }

            if (!string.IsNullOrEmpty(eventSchedule.ExternalEventKey))
            {
                throw new InvalidOperationException(
                    $"EventSchedule {eventScheduleId} already has ExternalEventKey " +
                    $"'{eventSchedule.ExternalEventKey}' - cannot add to SeasonPass bundle. " +
                    $"Events must be born inside a Seats.io season.");
            }

            var existingLinks = await bundleEventScheduleRepository.GetByEventScheduleIdAsync(eventScheduleId);
            if (existingLinks.Any(bes =>
                    bes.Bundle is not null &&
                    bes.Bundle.BundleType == BundleType.SeasonPass &&
                    bes.BundleId != bundle.Id))
            {
                throw new InvalidOperationException(
                    $"EventSchedule {eventScheduleId} already belongs to another SeasonPass bundle " +
                    $"(Seats.io 1-season-parent constraint).");
            }

            return eventSchedule;
        }

        private static void ValidateSelectableSchedule(
            Core.Model.Bundle bundle,
            Core.Model.EventSchedule eventSchedule)
        {
            if (eventSchedule.DeletedAt is not null)
            {
                throw new InvalidOperationException(
                    $"EventSchedule {eventSchedule.Id} is deleted and cannot be added to a Bundle.");
            }

            if (eventSchedule.StartDateTime < DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException(
                    $"EventSchedule {eventSchedule.Id} is in the past and cannot be added to a Bundle.");
            }

            if (!SelectableEventStatuses.Contains(eventSchedule.Event.Status))
            {
                throw new InvalidOperationException(
                    $"EventSchedule {eventSchedule.Id} parent event status is '{eventSchedule.Event.Status}'. " +
                    $"Allowed statuses: {string.Join(", ", SelectableEventStatuses)}.");
            }

            if (!SelectableScheduleStatuses.Contains(eventSchedule.Status))
            {
                throw new InvalidOperationException(
                    $"EventSchedule {eventSchedule.Id} status is '{eventSchedule.Status}'. " +
                    $"Allowed statuses: {string.Join(", ", SelectableScheduleStatuses)}.");
            }

            if (bundle.BundleType == BundleType.SeasonPass)
            {
                if (eventSchedule.Status != ScheduleStatus.Draft)
                {
                    throw new InvalidOperationException(
                        $"SeasonPass bundles can only include Draft event schedules. " +
                        $"EventSchedule {eventSchedule.Id} is '{eventSchedule.Status}'.");
                }

                return;
            }

            if (eventSchedule.Status == ScheduleStatus.OnSale &&
                string.IsNullOrWhiteSpace(eventSchedule.ExternalEventKey))
            {
                throw new InvalidOperationException(
                    $"EventSchedule {eventSchedule.Id} is OnSale but has no ExternalEventKey.");
            }
        }
    }
}
