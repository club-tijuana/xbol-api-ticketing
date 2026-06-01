using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class BundleUpdateRequest
    {
        public string? Name { get; set; }
        public string? Subtitle { get; set; }
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public string? BannerImageUrl { get; set; }
        public string? PosterImageUrl { get; set; }
        public string? LandingUrl { get; set; }
        public AgeRestriction? AgeRestriction { get; set; }
        public string? SecurityPolicies { get; set; }
        public string? AdditionalComments { get; set; }
        public EventStatus? Status { get; set; }
        public List<long>? CategoryIds { get; set; }

        public BundleType? BundleType { get; set; }
        public BundlePricingType? BundlePricingType { get; set; }
        public string? Code { get; set; }

        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public DateTimeOffset? PublishedDate { get; set; }
        public DateTimeOffset? OnSaleDate { get; set; }
        public DateTimeOffset? PreSaleDate { get; set; }
        public DateTimeOffset? OffSaleDate { get; set; }
        public DateTimeOffset? RenewalStartDate { get; set; }
        public DateTimeOffset? RenewalEndDate { get; set; }

        public long? PreviousBundleId { get; set; }
    }
}
