namespace XBOL.Ticketing.Core.Model
{
    public class EventSeat : BaseModel
    {
        public long EventSectionId { get; set; }
        public EventSection EventSection { get; set; } = null!;
        public long BaseSeatId { get; set; }
        public BaseSeat BaseSeat { get; set; } = null!;
        public string ExternalSeatObjectKey { get; set; } = null!;

        public IList<Ticket> Tickets { get; set; } = [];
        public IList<SeatHold> SeatHolds { get; set; } = [];
    }
}
