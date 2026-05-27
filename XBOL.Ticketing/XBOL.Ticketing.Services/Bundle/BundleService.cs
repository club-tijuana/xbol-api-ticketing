using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Core.Mappers;
using XBOL.Ticketing.Data.Abstractions;

namespace XBOL.Ticketing.Services.Bundle
{
    public class BundleService(IBundleRepository repository, IBaseSectionRepository baseSectionRepository)
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

            return new PagedResponse<BundleDTO>
            {
                Items = bundles.ToDto(),
                TotalCount = totalCount,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize
            };
        }

        public async Task<BundleDTO?> GetByIdAsync(long id)
        {
            var bundle = await Repository.GetByIdAsync(id);
            return bundle?.ToDto();
        }

        public async Task<BundleDTO> CreateAsync(BundleCreateRequest request, Guid userId)
        {
            var now = DateTimeOffset.UtcNow;
            var bundle = new Core.Model.Bundle
            {
                VenueMapId = request.VenueMapId,
                OrganizerId = request.OrganizerId,
                SeasonId = request.SeasonId,
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

            if (request.Name is not null) { bundle.Name = request.Name; }
            if (request.Subtitle is not null) { bundle.Subtitle = request.Subtitle; }
            if (request.ShortDescription is not null) { bundle.ShortDescription = request.ShortDescription; }
            if (request.LongDescription is not null) { bundle.LongDescription = request.LongDescription; }
            if (request.BannerImageUrl is not null) { bundle.BannerImageUrl = request.BannerImageUrl; }
            if (request.PosterImageUrl is not null) { bundle.PosterImageUrl = request.PosterImageUrl; }
            if (request.LandingUrl is not null) { bundle.LandingUrl = request.LandingUrl; }
            if (request.Status is not null) { bundle.Status = request.Status.Value; }
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

            await Repository.UpdateAsync(bundle);
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
