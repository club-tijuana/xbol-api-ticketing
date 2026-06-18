namespace XBOL.Ticketing.Core.Model
{
    public class ExchangeRate : BaseModel
    {
        public long OrganizerId { get; set; }
        public Organizer Organizer { get; set; } = null!;
        public decimal Rate { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset? DisabledAt { get; set; }
        public Guid? DisabledBy { get; set; }
    }
}
