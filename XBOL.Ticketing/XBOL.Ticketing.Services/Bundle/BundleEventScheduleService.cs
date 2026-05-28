using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Mappers;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;

namespace XBOL.Ticketing.Services.Bundle
{
    public class BundleEventScheduleService(
        IBundleEventScheduleRepository bundleEventScheduleRepository,
        IBundleRepository bundleRepository,
        IEventScheduleRepository eventScheduleRepository,
        IBundleLifecycleService lifecycleService)
    {
        private static readonly HashSet<EventStatus> EditableStatuses =
        [
            EventStatus.Draft,
            EventStatus.PendingReview,
            EventStatus.Approved
        ];

        public async Task<List<BundleEventScheduleResponseDTO>> GetByBundleAsync(long bundleId)
        {
            var bundle = await bundleRepository.GetByIdAsync(bundleId)
                ?? throw new KeyNotFoundException($"Bundle {bundleId} not found.");

            var entries = await bundleEventScheduleRepository.GetByBundleIdWithSchedulesAsync(bundleId);
            return entries.ToDto();
        }

        public async Task<List<BundleEventScheduleResponseDTO>> AddAsync(
            long bundleId, BundleEventScheduleAddRequest request)
        {
            var bundle = await bundleRepository.GetByIdAsync(bundleId)
                ?? throw new KeyNotFoundException($"Bundle {bundleId} not found.");

            ValidateBundleStatus(bundle, allowPublishedSeasonPassAdd: true);

            foreach (var item in request.Items)
            {
                await ValidateAdditionAsync(bundle, item.EventScheduleId);
            }

            foreach (var item in request.Items)
            {
                await bundleEventScheduleRepository.InsertAsync(new BundleEventSchedule
                {
                    BundleId = bundleId,
                    EventScheduleId = item.EventScheduleId,
                    SortOrder = item.SortOrder
                });
            }

            await bundleEventScheduleRepository.CommitAsync();

            await lifecycleService.AddSchedulesAsync(
                bundleId,
                request.Items.Select(item => item.EventScheduleId).ToArray());

            var entries = await bundleEventScheduleRepository.GetByBundleIdWithSchedulesAsync(bundleId);
            return entries.ToDto();
        }

        public async Task<bool> RemoveAsync(long bundleId, long eventScheduleId)
        {
            var bundle = await bundleRepository.GetByIdAsync(bundleId)
                ?? throw new KeyNotFoundException($"Bundle {bundleId} not found.");

            ValidateBundleStatus(bundle);

            var entry = await bundleEventScheduleRepository.GetByCompositeKeyAsync(bundleId, eventScheduleId)
                ?? throw new KeyNotFoundException(
                    $"BundleEventSchedule ({bundleId}, {eventScheduleId}) not found.");

            bundleEventScheduleRepository.Remove(entry);
            await bundleEventScheduleRepository.CommitAsync();
            return true;
        }

        public async Task<int> RemoveBatchAsync(long bundleId, BundleEventScheduleRemoveRequest request)
        {
            var bundle = await bundleRepository.GetByIdAsync(bundleId)
                ?? throw new KeyNotFoundException($"Bundle {bundleId} not found.");

            ValidateBundleStatus(bundle);

            var removed = 0;
            foreach (var eventScheduleId in request.EventScheduleIds)
            {
                var entry = await bundleEventScheduleRepository.GetByCompositeKeyAsync(bundleId, eventScheduleId);
                if (entry is not null)
                {
                    bundleEventScheduleRepository.Remove(entry);
                    removed++;
                }
            }

            if (removed > 0)
            {
                await bundleEventScheduleRepository.CommitAsync();
            }

            return removed;
        }

        private void ValidateBundleStatus(
            Core.Model.Bundle bundle,
            bool allowPublishedSeasonPassAdd = false)
        {
            if (!EditableStatuses.Contains(bundle.Status))
            {
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
        }

        private async Task ValidateAdditionAsync(Core.Model.Bundle bundle, long eventScheduleId)
        {
            var eventSchedule = await eventScheduleRepository.GetByIdAsync(eventScheduleId)
                ?? throw new KeyNotFoundException($"EventSchedule {eventScheduleId} not found.");

            if (await bundleEventScheduleRepository.ExistsAsync(bundle.Id, eventScheduleId))
            {
                throw new InvalidOperationException(
                    $"EventSchedule {eventScheduleId} is already in Bundle {bundle.Id}.");
            }

            if (bundle.BundleType == BundleType.SeasonPass)
            {
                if (!string.IsNullOrEmpty(eventSchedule.ExternalEventKey))
                {
                    throw new InvalidOperationException(
                        $"EventSchedule {eventScheduleId} already has ExternalEventKey " +
                        $"'{eventSchedule.ExternalEventKey}' — cannot add to SeasonPass bundle. " +
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
            }
        }
    }
}
