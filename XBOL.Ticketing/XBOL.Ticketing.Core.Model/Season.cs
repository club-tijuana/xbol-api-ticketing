using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class Season : BaseModel
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Description { get; set; } = null!;

        public string BannerImageUrl { get; set; } = null!;
        public string PosterImageUrl { get; set; } = null!;
        public string LandingUrl { get; set; } = null!;

        public DateTimeOffset PublishedDate { get; set; }
        public DateTimeOffset OnSaleDate { get; set; }
        public DateTimeOffset PreSaleDate { get; set; }
        public DateTimeOffset OffSaleDate { get; set; }

        public SeasonStatus Status { get; set; }

        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        public string ExternalSeasonKey { get; set; } = null!;

        public IList<SeasonPass> SeasonPasses { get; set; } = [];
        public IList<SeasonTag> SeasonTags { get; set; } = [];
    }
}
