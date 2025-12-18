namespace XBOL.Ticketing.Core.Model
{
    public class AuditLog
    {
        public long Id { get; set; }

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public string ActionType { get; set; } = null!;
        public string EntityName { get; set; } = null!;
        public long EntityId { get; set; }

        public string PayloadBefore { get; set; } = null!;
        public string PayloadAfter { get; set; } = null!;

        public DateTimeOffset PerformedAt { get; set; }
    }
}