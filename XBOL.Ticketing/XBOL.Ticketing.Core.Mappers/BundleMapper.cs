using EntityDTO = XBOL.Ticketing.Core.DTO.BundleDTO;
using EntityModel = XBOL.Ticketing.Core.Model.Bundle;

namespace XBOL.Ticketing.Core.Mappers
{
    public static class BundleMapper
    {
        public static List<EntityDTO> ToDto(this IList<EntityModel> entities)
            => [.. entities.Select(x => x.ToDto())];

        public static EntityDTO ToDto(this EntityModel entity)
        {
            return new EntityDTO
            {
                Id = entity.Id,
                VenueMapId = entity.VenueMapId,
                SeasonId = entity.SeasonId,
                Name = entity.Name,
                Subtitle = entity.Subtitle ?? string.Empty,
                ShortDescription = entity.ShortDescription ?? string.Empty,
                LongDescription = entity.LongDescription ?? string.Empty,
                BannerImageUrl = entity.BannerImageUrl ?? string.Empty,
                PosterImageUrl = entity.PosterImageUrl ?? string.Empty,
                LandingUrl = entity.LandingUrl ?? string.Empty,
                Status = entity.Status,
                BundleType = entity.BundleType,
                BundlePricingType = entity.BundlePricingType,
                Code = entity.Code,
                ExternalKey = entity.ExternalKey,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                PublishedDate = entity.PublishedDate,
                OnSaleDate = entity.OnSaleDate,
                PreSaleDate = entity.PreSaleDate,
                OffSaleDate = entity.OffSaleDate,
                RenewalStartDate = entity.RenewalStartDate,
                RenewalEndDate = entity.RenewalEndDate,
                PreviousBundleId = entity.PreviousBundleId,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CreatedBy = entity.CreatedBy,
                UpdatedBy = entity.UpdatedBy
            };
        }

        public static List<EntityModel> ToModel(this IList<EntityDTO> entities)
            => [.. entities.Select(x => x.ToModel())];

        public static EntityModel ToModel(this EntityDTO entity)
        {
            return new EntityModel
            {
                Id = entity.Id,
                VenueMapId = entity.VenueMapId,
                OrganizerId = entity.OrganizerId,
                SeasonId = entity.SeasonId,
                Name = entity.Name,
                Subtitle = entity.Subtitle ?? string.Empty,
                ShortDescription = entity.ShortDescription ?? string.Empty,
                LongDescription = entity.LongDescription ?? string.Empty,
                BannerImageUrl = entity.BannerImageUrl ?? string.Empty,
                PosterImageUrl = entity.PosterImageUrl ?? string.Empty,
                LandingUrl = entity.LandingUrl ?? string.Empty,
                Status = entity.Status,
                BundleType = entity.BundleType,
                BundlePricingType = entity.BundlePricingType,
                Code = entity.Code,
                ExternalKey = entity.ExternalKey,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                PublishedDate = entity.PublishedDate,
                OnSaleDate = entity.OnSaleDate,
                PreSaleDate = entity.PreSaleDate,
                OffSaleDate = entity.OffSaleDate,
                RenewalStartDate = entity.RenewalStartDate,
                RenewalEndDate = entity.RenewalEndDate,
                PreviousBundleId = entity.PreviousBundleId,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CreatedBy = entity.CreatedBy,
                UpdatedBy = entity.UpdatedBy
            };
        }
    }
}
