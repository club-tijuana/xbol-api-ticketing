namespace XBOL.Ticketing.Core.Model
{
    public class EventSeat
    {
        public long Id { get; set; }

        public long EventSectionId { get; set; }
        public EventSection EventSection { get; set; } = null!;

        public long BaseSeatId { get; set; }
        public BaseSeat BaseSeat { get; set; } = null!;

        public decimal? PriceOverride { get; set; }
        public string ExternalSeatObjectKey { get; set; } = null!;

        public IList<PriceRule> PriceRules { get; set; } = [];
        public IList<Ticket> Tickets { get; set; } = [];
        public IList<SeatHold> SeatHolds { get; set; } = [];
    }
}