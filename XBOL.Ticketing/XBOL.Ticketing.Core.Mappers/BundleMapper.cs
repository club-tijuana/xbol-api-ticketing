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
                Subtitle = entity.Subtitle,
                ShortDescription = entity.ShortDescription,
                LongDescription = entity.LongDescription,
                BannerImageUrl = entity.BannerImageUrl,
                PosterImageUrl = entity.PosterImageUrl,
                LandingUrl = entity.LandingUrl,
                AgeRestriction = entity.AgeRestriction,
                SecurityPolicies = entity.SecurityPolicies,
                AdditionalComments = entity.AdditionalComments,
                Status = entity.Status,
                VenueId = entity.VenueMap?.VenueId,
                VenueName = entity.VenueMap?.Venue?.Name,
                IsSeason = entity.BundleType == Core.Commons.Enums.BundleType.SeasonPass,
                Categories = entity.Categories?.Select(c => new DTO.Results.EventCategoryResult
                {
                    Id = c.Id,
                    Name = c.Name,
                    DisplayName = c.DisplayName,
                    IsActive = c.IsActive
                }).ToList() ?? [],
                Schedules = [],
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
                VenueMapId = entity.VenueMapId ?? 0,
                OrganizerId = entity.OrganizerId,
                SeasonId = entity.SeasonId,
                Name = entity.Name,
                Subtitle = entity.Subtitle,
                ShortDescription = entity.ShortDescription,
                LongDescription = entity.LongDescription,
                BannerImageUrl = entity.BannerImageUrl,
                PosterImageUrl = entity.PosterImageUrl,
                LandingUrl = entity.LandingUrl,
                AgeRestriction = entity.AgeRestriction,
                SecurityPolicies = entity.SecurityPolicies,
                AdditionalComments = entity.AdditionalComments,
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
