namespace XBOL.Ticketing.Core.Model
{
    public class OrganizerMember
    {
        public long Id { get; set; }

        public long OrganizerId { get; set; }
        public Organizer Organizer { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}