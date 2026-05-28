using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Core.Mappers;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Data.Repositories.Media;
using XBOL.Ticketing.Services.Media;

namespace XBOL.Ticketing.Services.Bundle
{
    public class BundleService(IBundleRepository repository,
                               IBaseSectionRepository baseSectionRepository,
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

            var bannerMedia = await mediaRepository
                .Get()
                .AsNoTracking()
                .Where(m => m.ReferenceType == SaleType.Bundle
                         && m.DeletedAt == null
                         && bundleIds.Contains(m.ReferenceId)
                         && (m.MediaType == MediaType.Banner || m.MediaType == MediaType.Logo))
                .GroupBy(m => m.ReferenceId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.ToDictionary(m => m.MediaType, m => m)
                );

            var dtos = bundles.ToDto();

            foreach (var dto in dtos)
            {
                if (bannerMedia.TryGetValue(dto.Id, out var mediaByType))
                {
                    if (mediaByType.TryGetValue(MediaType.Banner, out var banner))
                    {
                        dto.BannerImageUrl = $"data:{banner.ContentType};base64,{Convert.ToBase64String(banner.Content)}";
                    }
                    if (mediaByType.TryGetValue(MediaType.Logo, out var poster))
                    {
                        dto.PosterImageUrl = $"data:{poster.ContentType};base64,{Convert.ToBase64String(poster.Content)}";
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
            var bundle = await Repository.GetByIdAsync(id);
            if (bundle is null) { return null; }

            var dto = bundle.ToDto();
            dto.Media = await mediaService.GetProductMediaAsync(id, SaleType.Bundle);

            var mediaByType = dto.Media
                .Where(m => m.MediaType == MediaType.Banner || m.MediaType == MediaType.Logo)
                .ToDictionary(m => m.MediaType);

            if (mediaByType.TryGetValue(MediaType.Banner, out var banner))
            {
                dto.BannerImageUrl = $"data:{banner.ContentType};base64,{banner.ImageBase64}";
            }
            if (mediaByType.TryGetValue(MediaType.Logo, out var poster))
            {
                dto.PosterImageUrl = $"data:{poster.ContentType};base64,{poster.ImageBase64}";
            }

            return dto;
        }

        public async Task<BundleDTO> CreateAsync(BundleCreateRequest request, Guid userId)
        {
            var now = DateTimeOffset.UtcNow;
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
                Status = EventStatus.Draft,
                BundleType = request.BundleType,
                BundlePricingType = request.BundlePricingType,
                Code = request.Code,
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

            if (request.BundlePricingType == BundlePricingType.Single)
            {
                var baseSections = baseSectionRepository.Get(
                    includedProperties: "BaseZone"
                ).Where(x => x.BaseZone.VenueMapId == request.VenueMapId).ToList();

                bundle.BundleSections = baseSections.Select(x => new Core.Model.BundleSection
                {
                    BaseSectionId = x.Id,
                    Price = 0,
                    TotalSeats = 0,
                    AvailableSeats = 0,
                    DisplayName = string.Empty
                }).ToList();
            }

            await Repository.InsertAsync(bundle);
            await Repository.CommitAsync();
            return bundle.ToDto();
        }

        public async Task<BundleDTO?> UpdateAsync(long id, BundleUpdateRequest request, Guid userId)
        {
            var bundle = await Repository.GetByIdAsync(id);
            if (bundle is null) { return null; }
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

            if (request.Status is not null)
            {
                EventStatusTransitions.ValidateTransition(bundle.Status, request.Status.Value);
                if (!publishRequested && !cancelRequested) { bundle.Status = request.Status.Value; }
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

        public async Task<bool> DeleteAsync(long id)
        {
            var bundle = await Repository.GetByIdAsync(id);
            if (bundle is null) { return false; }

            await Repository.HardDeleteAsync(bundle);
            return true;
        }
    }
}
