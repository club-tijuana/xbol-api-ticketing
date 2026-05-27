using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO
{
    public class BundleDTO
    {
        public long Id { get; set; }

        // Event fields
        public long VenueMapId { get; set; }
        public long OrganizerId { get; set; }
        public long? SeasonId { get; set; }
        public string Name { get; set; } = null!;
        public string Subtitle { get; set; } = null!;
        public string ShortDescription { get; set; } = null!;
        public string LongDescription { get; set; } = null!;
        public string? BannerImageUrl { get; set; }
        public string? PosterImageUrl { get; set; }
        public string? LandingUrl { get; set; }
        public EventStatus Status { get; set; }

        // Bundle-specific fields
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

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}
