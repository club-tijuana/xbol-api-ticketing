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
                OrganizerId = entity.OrganizerId ?? 0,
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
                Schedules = entity.BundleEventSchedules
                    .Where(link => link.EventSchedule is not null)
                    .OrderBy(link => link.SortOrder ?? int.MaxValue)
                    .ThenBy(link => link.EventSchedule.StartDateTime)
                    .Select(link => ToScheduleDto(link.EventSchedule))
                    .ToList(),
                BundleSaleWindow = ToSaleWindowDto(entity),
                BundleType = entity.BundleType,
                BundlePricingType = entity.BundlePricingType,
                Code = entity.Code,
                ExternalKey = entity.ExternalKey,
                IsBookable = BundleBookability.IsBookable(entity),
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

        private static DTO.EventScheduleDTO ToScheduleDto(Core.Model.EventSchedule schedule)
        {
            return new DTO.EventScheduleDTO
            {
                Id = schedule.Id,
                StartDateTime = schedule.StartDateTime,
                EndDateTime = schedule.EndDateTime,
                PublishedDate = schedule.PublishedDate,
                PreSaleStartDate = schedule.PreSaleStartDate,
                PreSaleEndDate = schedule.PreSaleEndDate,
                OnSaleDate = schedule.OnSaleDate,
                OffSaleDate = schedule.OffSaleDate,
                GateOpenDate = schedule.GateOpenDate,
                ExternalEventKey = schedule.ExternalEventKey,
                TotalSeats = schedule.Sections.Sum(section => section.TotalSeats),
                AvailableSeats = schedule.Sections.Sum(section => section.AvailableSeats),
                Status = schedule.Status
            };
        }

        private static DTO.BundleSaleWindowDTO ToSaleWindowDto(EntityModel entity)
        {
            return new DTO.BundleSaleWindowDTO
            {
                BundleScheduleKey = $"bundle-sale-window:{entity.Id}",
                BundleId = entity.Id,
                PreviousBundleId = entity.PreviousBundleId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                PublishedDate = entity.PublishedDate,
                OnSaleDate = entity.OnSaleDate,
                PreSaleDate = entity.PreSaleDate,
                OffSaleDate = entity.OffSaleDate,
                RenewalStartDate = entity.RenewalStartDate,
                RenewalEndDate = entity.RenewalEndDate,
                ExternalKey = entity.ExternalKey
            };
        }

        public static EntityModel ToModel(this EntityDTO entity)
        {
            return new EntityModel
            {
                Id = entity.Id,
                VenueMapId = entity.VenueMapId ?? 0,
                OrganizerId = entity.OrganizerId,
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
