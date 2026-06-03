using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Core.Mappers;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Data.Queries;
using XBOL.Ticketing.Data.Repositories.Media;
using XBOL.Ticketing.Services.Media;

namespace XBOL.Ticketing.Services.Bundle
{
    public class BundleService(IBundleRepository repository,
                               IBaseSectionRepository baseSectionRepository,
                               IBundleEventScheduleRepository bundleEventScheduleRepository,
                               IEventScheduleRepository eventScheduleRepository,
                               MediaRepository mediaRepository,
                               MediaService mediaService,
                               IBundleLifecycleService lifecycleService)
    {
        protected IBundleRepository Repository { get; set; } = repository;

        public async Task<PagedResponse<BundleDTO>> GetPagedAsync(BundleQueryParams queryParams)
        {
            var searchTerm = queryParams.SearchTerm?.Trim().ToLower() ?? "";

            var bundles = Repository.Get(
                filter: string.IsNullOrEmpty(searchTerm)
                    ? null
                    : b => b.Name.ToLower().Contains(searchTerm),
                orderBy: q => q.OrderBy(b => b.Id),
                pageSize: queryParams.PageSize,
                currentPage: queryParams.Page
            ).ToList();

            var totalCount = Repository.Get().Count();

            var bundleIds = bundles.Select(b => b.Id).ToList();

            var media = await mediaRepository
                .Get()
                .AsNoTracking()
                .Include(m => m.BlobAsset)
                .Where(m => m.ReferenceType == SaleType.Bundle
                         && bundleIds.Contains(m.ReferenceId)
                         && (m.MediaType == MediaType.Banner || m.MediaType == MediaType.Logo))
                .AvailableBlobMedia()
                .OrderBy(m => m.ReferenceId)
                .ThenBy(m => m.MediaType)
                .ThenBy(m => m.Order)
                .ThenBy(m => m.Id)
                .ToListAsync();

            var bannerMedia = media
                .GroupBy(m => m.ReferenceId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(m => m.MediaType).ToDictionary(m => m.Key, m => m.First())
                );

            var dtos = bundles.ToDto();

            foreach (var dto in dtos)
            {
                if (bannerMedia.TryGetValue(dto.Id, out var mediaByType))
                {
                    if (mediaByType.TryGetValue(MediaType.Banner, out var banner))
                    {
                        dto.BannerImageUrl = banner.BlobAsset.Url ?? dto.BannerImageUrl;
                    }
                    if (mediaByType.TryGetValue(MediaType.Logo, out var poster))
                    {
                        dto.PosterImageUrl = poster.BlobAsset.Url ?? dto.PosterImageUrl;
                    }
                }
            }

            return new PagedResponse<BundleDTO>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize
            };
        }

        public async Task<BundleDTO?> GetByIdAsync(long id)
        {
            var bundle = await Repository.GetByIdWithVenueMapAndSchedulesAsync(id);
            if (bundle is null) { return null; }

            var dto = bundle.ToDto();
            dto.Media = await mediaService.GetProductMediaAsync(id, SaleType.Bundle);

            var mediaByType = dto.Media
                .Where(m => m.MediaType == MediaType.Banner || m.MediaType == MediaType.Logo)
                .GroupBy(m => m.MediaType)
                .ToDictionary(g => g.Key, g => g.First());

            if (mediaByType.TryGetValue(MediaType.Banner, out var banner))
            {
                dto.BannerImageUrl = banner.Url ?? dto.BannerImageUrl;
            }
            if (mediaByType.TryGetValue(MediaType.Logo, out var poster))
            {
                dto.PosterImageUrl = poster.Url ?? dto.PosterImageUrl;
            }

            return dto;
        }

        public async Task<BundleDTO> CreateAsync(BundleCreateRequest request, Guid userId)
        {
            ValidatePricingCombination(request);

            var now = DateTimeOffset.UtcNow;
            var categories = await Repository.GetCategoriesByIdsAsync(request.CategoryIds);
            var bundle = new Core.Model.Bundle
            {
                VenueMapId = request.VenueMapId,
                OrganizerId = request.OrganizerId,
                Name = request.Name,
                Subtitle = request.Subtitle,
                ShortDescription = request.ShortDescription,
                LongDescription = request.LongDescription,
                BannerImageUrl = request.BannerImageUrl ?? string.Empty,
                PosterImageUrl = request.PosterImageUrl ?? string.Empty,
                LandingUrl = request.LandingUrl ?? string.Empty,
                AgeRestriction = request.AgeRestriction,
                SecurityPolicies = request.SecurityPolicies,
                AdditionalComments = request.AdditionalComments,
                Status = EventStatus.Draft,
                Categories = categories,
                BundleType = request.BundleType,
                BundlePricingType = request.BundlePricingType,
                Code = request.Code,
                BundleEventSchedules = [],
                StartDate = request.StartDate.HasValue ? request.StartDate.Value.ToUniversalTime() : null,
                EndDate = request.EndDate.HasValue ? request.EndDate.Value.ToUniversalTime() : null,
                PublishedDate = request.PublishedDate.HasValue ? request.PublishedDate.Value.ToUniversalTime() : null,
                OnSaleDate = request.OnSaleDate.HasValue ? request.OnSaleDate.Value.ToUniversalTime() : null,
                PreSaleDate = request.PreSaleDate.HasValue ? request.PreSaleDate.Value.ToUniversalTime() : null,
                OffSaleDate = request.OffSaleDate.HasValue ? request.OffSaleDate.Value.ToUniversalTime() : null,
                RenewalStartDate = request.RenewalStartDate.HasValue ? request.RenewalStartDate.Value.ToUniversalTime() : null,
                RenewalEndDate = request.RenewalEndDate.HasValue ? request.RenewalEndDate.Value.ToUniversalTime() : null,
                PreviousBundleId = request.PreviousBundleId,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            if (request.EventScheduleIds is not { Count: > 0 })
            {
                throw new InvalidOperationException("At least one event schedule must be selected for this Bundle.");
            }

            var duplicateEventScheduleId = request.EventScheduleIds
                .GroupBy(id => id)
                .FirstOrDefault(group => group.Count() > 1)
                ?.Key;
            if (duplicateEventScheduleId is not null)
            {
                throw new InvalidOperationException(
                    $"EventSchedule {duplicateEventScheduleId} is already selected for this Bundle.");
            }

            var eventSchedules = new List<Core.Model.EventSchedule>();
            foreach (var eventScheduleId in request.EventScheduleIds)
            {
                var eventSchedule = await BundleEventScheduleValidator.ValidateAdditionAsync(
                    bundle,
                    eventScheduleId,
                    bundleEventScheduleRepository,
                    eventScheduleRepository);
                eventSchedules.Add(eventSchedule);
            }

            bundle.BundleEventSchedules = eventSchedules
                .Select((eventSchedule, index) => new Core.Model.BundleEventSchedule
                {
                    EventScheduleId = eventSchedule.Id,
                    EventSchedule = eventSchedule,
                    SortOrder = index
                })
                .ToList();

            if (request.BundleType == BundleType.SeasonPass &&
                request.BundlePricingType == BundlePricingType.Single)
            {
                var baseSections = GetBaseSectionsForVenueMap(request.VenueMapId, includeSeats: false);

                bundle.BundleSections = baseSections.Select(section => new Core.Model.BundleSection
                {
                    BaseSectionId = section.Id,
                    TotalSeats = 0,
                    AvailableSeats = 0,
                    DisplayName = string.Empty
                }).ToList();
            }

            if (request.BundleType == BundleType.Basic &&
                request.BundlePricingType == BundlePricingType.Composite)
            {
                var baseSections = GetBaseSectionsForVenueMap(request.VenueMapId, includeSeats: true);
                bundle.BundleSections = baseSections
                    .Select(CreateBasicBundleSection)
                    .ToList();
            }

            await Repository.InsertAsync(bundle);
            await Repository.CommitAsync();
            return bundle.ToDto();
        }

        private List<Core.Model.BaseSection> GetBaseSectionsForVenueMap(long venueMapId, bool includeSeats)
        {
            var includedProperties = includeSeats
                ? new[] { "BaseZone", "BaseRows.BaseSeats" }
                : ["BaseZone"];

            return baseSectionRepository.Get(includedProperties: includedProperties)
                .Where(section => section.BaseZone.VenueMapId == venueMapId)
                .OrderBy(section => section.Id)
                .ToList();
        }

        private static Core.Model.BundleSection CreateBasicBundleSection(Core.Model.BaseSection baseSection)
        {
            var bundleSeats = baseSection.BaseRows
                .OrderBy(row => row.Id)
                .SelectMany(row => row.BaseSeats
                    .OrderBy(seat => seat.Id)
                    .Select(seat => new Core.Model.BundleSeat
                    {
                        BaseSeatId = seat.Id,
                        ExternalSeatObjectKey = BuildExternalSeatObjectKey(baseSection, row, seat),
                        ForSale = true
                    }))
                .ToList();

            return new Core.Model.BundleSection
            {
                BaseSectionId = baseSection.Id,
                TotalSeats = bundleSeats.Count,
                AvailableSeats = bundleSeats.Count,
                DisplayName = baseSection.Name,
                BundleSeats = bundleSeats
            };
        }

        private static string BuildExternalSeatObjectKey(
            Core.Model.BaseSection baseSection,
            Core.Model.BaseRow baseRow,
            Core.Model.BaseSeat baseSeat)
        {
            var keyParts = new[] { baseSection.Name, baseRow.RowLabel, baseSeat.SeatNumber }
                .Where(value => !string.IsNullOrWhiteSpace(value));

            return string.Join("-", keyParts);
        }

        private static void ValidatePricingCombination(BundleCreateRequest request)
        {
            if (request.BundleType is not (BundleType.Basic or BundleType.SeasonPass))
            {
                throw new InvalidOperationException("Bundle type must be Basic or Season Pass.");
            }

            if (request.BundleType == BundleType.Basic &&
                request.BundlePricingType != BundlePricingType.Composite)
            {
                throw new InvalidOperationException("Basic bundles must use Composite pricing.");
            }

            if (request.BundleType == BundleType.SeasonPass &&
                request.BundlePricingType != BundlePricingType.Single)
            {
                throw new InvalidOperationException("SeasonPass bundles must use Single pricing.");
            }
        }

        public async Task<BundleDTO?> UpdateAsync(long id, BundleUpdateRequest request, Guid userId)
        {
            var bundle = await Repository.GetByIdAsync(id);
            if (bundle is null) { return null; }
            ValidateClassificationUpdate(bundle, request);

            var publishRequested = request.Status == EventStatus.Published;
            var cancelRequested = request.Status == EventStatus.Cancelled;
            var syncSeasonMetadata =
                bundle.Status == EventStatus.Published &&
                bundle.BundleType == BundleType.SeasonPass &&
                request.Name is not null &&
                request.Name != bundle.Name;

            if (request.Name is not null) { bundle.Name = request.Name; }
            if (request.Subtitle is not null) { bundle.Subtitle = request.Subtitle; }
            if (request.ShortDescription is not null) { bundle.ShortDescription = request.ShortDescription; }
            if (request.LongDescription is not null) { bundle.LongDescription = request.LongDescription; }
            if (request.BannerImageUrl is not null) { bundle.BannerImageUrl = request.BannerImageUrl; }
            if (request.PosterImageUrl is not null) { bundle.PosterImageUrl = request.PosterImageUrl; }
            if (request.LandingUrl is not null) { bundle.LandingUrl = request.LandingUrl; }
            if (request.AgeRestriction is not null) { bundle.AgeRestriction = request.AgeRestriction; }
            if (request.SecurityPolicies is not null) { bundle.SecurityPolicies = request.SecurityPolicies; }
            if (request.AdditionalComments is not null) { bundle.AdditionalComments = request.AdditionalComments; }

            if (request.Status is not null)
            {
                EventStatusTransitions.ValidateTransition(bundle.Status, request.Status.Value);
                if (!publishRequested && !cancelRequested) { bundle.Status = request.Status.Value; }
            }

            if (request.CategoryIds is not null)
            {
                var categories = await Repository.GetCategoriesByIdsAsync(request.CategoryIds);
                bundle.Categories.Clear();
                foreach (var category in categories)
                {
                    bundle.Categories.Add(category);
                }
            }

            if (request.BundleType is not null) { bundle.BundleType = request.BundleType.Value; }
            if (request.BundlePricingType is not null) { bundle.BundlePricingType = request.BundlePricingType.Value; }
            if (request.Code is not null) { bundle.Code = request.Code; }
            if (request.StartDate.HasValue) { bundle.StartDate = request.StartDate.Value.ToUniversalTime(); }
            if (request.EndDate.HasValue) { bundle.EndDate = request.EndDate.Value.ToUniversalTime(); }
            if (request.PublishedDate.HasValue) { bundle.PublishedDate = request.PublishedDate.Value.ToUniversalTime(); }
            if (request.OnSaleDate.HasValue) { bundle.OnSaleDate = request.OnSaleDate.Value.ToUniversalTime(); }
            if (request.PreSaleDate.HasValue) { bundle.PreSaleDate = request.PreSaleDate.Value.ToUniversalTime(); }
            if (request.OffSaleDate.HasValue) { bundle.OffSaleDate = request.OffSaleDate.Value.ToUniversalTime(); }
            if (request.RenewalStartDate.HasValue) { bundle.RenewalStartDate = request.RenewalStartDate.Value.ToUniversalTime(); }
            if (request.RenewalEndDate.HasValue) { bundle.RenewalEndDate = request.RenewalEndDate.Value.ToUniversalTime(); }
            if (request.PreviousBundleId is not null) { bundle.PreviousBundleId = request.PreviousBundleId; }

            bundle.UpdatedAt = DateTimeOffset.UtcNow;
            bundle.UpdatedBy = userId;

            if (publishRequested)
            {
                await lifecycleService.PublishAsync(id, userId);
                var publishedBundle = Repository.Get(filter: b => b.Id == id).FirstOrDefault() ?? bundle;
                return publishedBundle.ToDto();
            }

            if (cancelRequested)
            {
                await lifecycleService.CancelAsync(id, userId);
                bundle.Status = EventStatus.Cancelled;
                bundle.ExternalKey = null;
                return bundle.ToDto();
            }

            await Repository.UpdateAsync(bundle);
            if (syncSeasonMetadata)
            {
                await lifecycleService.SyncMetadataAsync(id);
            }

            return bundle.ToDto();
        }

        private static void ValidateClassificationUpdate(Core.Model.Bundle bundle, BundleUpdateRequest request)
        {
            if (request.BundleType is not null && request.BundleType.Value != bundle.BundleType)
            {
                throw new InvalidOperationException("BundleType cannot be changed after bundle creation.");
            }

            if (request.BundlePricingType is not null && request.BundlePricingType.Value != bundle.BundlePricingType)
            {
                throw new InvalidOperationException("BundlePricingType cannot be changed after bundle creation.");
            }
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var bundle = await Repository.GetByIdAsync(id);
            if (bundle is null) { return false; }

            await Repository.HardDeleteAsync(bundle);
            return true;
        }
    }
}
