using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Core.DTO.Results;

namespace XBOL.Ticketing.Core.DTO
{
    public class BundleDTO
    {
        public long Id { get; set; }

        public long? VenueMapId { get; set; }
        public long OrganizerId { get; set; }
        public string Name { get; set; } = null!;
        public string? Subtitle { get; set; }
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public string? BannerImageUrl { get; set; }
        public string? PosterImageUrl { get; set; }
        public string? LandingUrl { get; set; }
        public AgeRestriction? AgeRestriction { get; set; }
        public string? SecurityPolicies { get; set; }
        public string? AdditionalComments { get; set; }
        public EventStatus Status { get; set; }

        public long? VenueId { get; set; }
        public string? VenueName { get; set; }
        public bool IsSeason { get; set; }
        public List<EventCategoryResult> Categories { get; set; } = [];
        public IList<EventScheduleDTO> Schedules { get; set; } = [];
        public BundleSaleWindowDTO? BundleSaleWindow { get; set; }

        public BundleType BundleType { get; set; }
        public BundlePricingType BundlePricingType { get; set; }
        public string? Code { get; set; }
        public string? ExternalKey { get; set; }

        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public DateTimeOffset? PublishedDate { get; set; }
        public DateTimeOffset? OnSaleDate { get; set; }
        public DateTimeOffset? PreSaleDate { get; set; }
        public DateTimeOffset? OffSaleDate { get; set; }
        public DateTimeOffset? RenewalStartDate { get; set; }
        public DateTimeOffset? RenewalEndDate { get; set; }

        public long? PreviousBundleId { get; set; }

        public IList<MediaResponse> Media { get; set; } = [];

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
