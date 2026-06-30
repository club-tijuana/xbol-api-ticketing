using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Mappers;
using XBOL.Ticketing.Core.Commons.Enums;
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

            BundleEventScheduleValidator.ValidateBundleStatus(bundle, allowPublishedSeasonPassAdd: true);

            foreach (var item in request.Items)
            {
                await BundleEventScheduleValidator.ValidateAdditionAsync(
                    bundle,
                    item.EventScheduleId,
                    bundleEventScheduleRepository,
                    eventScheduleRepository);
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

            if (bundle.BundleType != BundleType.SeasonPass ||
                bundle.Status != EventStatus.Published)
            {
                await lifecycleService.AddSchedulesAsync(
                    bundleId,
                    request.Items.Select(item => item.EventScheduleId).ToArray());
            }

            var entries = await bundleEventScheduleRepository.GetByBundleIdWithSchedulesAsync(bundleId);
            return entries.ToDto();
        }

        public async Task<bool> RemoveAsync(long bundleId, long eventScheduleId)
        {
            var bundle = await bundleRepository.GetByIdAsync(bundleId)
                ?? throw new KeyNotFoundException($"Bundle {bundleId} not found.");

            BundleEventScheduleValidator.ValidateBundleStatus(bundle);

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

            BundleEventScheduleValidator.ValidateBundleStatus(bundle);

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
    }
}
