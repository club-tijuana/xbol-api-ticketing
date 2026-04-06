using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class Venue : BaseModel
    {
        public string Name { get; set; } = null!;
        public string AddressLine { get; set; } = null!;
        public string City { get; set; } = null!;
        public string State { get; set; } = null!;
        public string Country { get; set; } = null!;
        public string? ZipCode { get; set; }

        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public VenueCategory Category { get; set; }

        public string ShortDescription { get; set; } = null!;
        public string LongDescription { get; set; } = null!;

        public string LogoImageUrl { get; set; } = null!;
        public string BannerImageUrl { get; set; } = null!;
        public string LandingUrl { get; set; } = null!;

        public string? ContactEmail { get; set; }
        public long? PhoneRegionCodeId { get; set; }
        public PhoneRegionCode? PhoneRegionCode { get; set; }
        public string? ContactPhoneNumber { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        public IList<VenueMap> VenueMaps { get; set; } = [];
    }
}
