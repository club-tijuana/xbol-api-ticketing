using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class Event : BaseModel
    {
        public long VenueMapId { get; set; }
        public VenueMap VenueMap { get; set; } = null!;

        public long OrganizerId { get; set; }
        public Organizer Organizer { get; set; } = null!;

        public long? SeasonId { get; set; }
        public Season? Season { get; set; }

        public string Name { get; set; } = null!;
        public string Subtitle { get; set; } = null!;
        public IList<EventCategory> Categories { get; set; } = new List<EventCategory>();

        public string ShortDescription { get; set; } = null!;
        public string LongDescription { get; set; } = null!;

        public string BannerImageUrl { get; set; } = null!;
        public string PosterImageUrl { get; set; } = null!;
        public string LandingUrl { get; set; } = null!;

        public EventStatus Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        public IList<EventSchedule> Schedules { get; set; } = [];
        public IList<EventMedia> Media { get; set; } = [];
        public IList<EventTag> Tags { get; set; } = [];
    }
}
