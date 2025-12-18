namespace XBOL.Ticketing.Core.Model
{
    public class EventTag
    {
        public long Id { get; set; }

        public long EventId { get; set; }
        public Event Event { get; set; } = null!;

        public long TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}