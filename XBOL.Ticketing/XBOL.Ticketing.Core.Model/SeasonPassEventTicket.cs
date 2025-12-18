namespace XBOL.Ticketing.Core.Model
{
    public class SeasonPassEventTicket
    {
        public long Id { get; set; }

        public long SeasonPassId { get; set; }
        public SeasonPass SeasonPass { get; set; } = null!;

        public long TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;
    }
}