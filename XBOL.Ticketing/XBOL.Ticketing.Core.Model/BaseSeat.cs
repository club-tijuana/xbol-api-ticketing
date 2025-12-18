using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class BaseSeat
    {
        public long Id { get; set; }

        public long BaseRowId { get; set; }
        public BaseRow BaseRow { get; set; } = null!;

        public string SeatNumber { get; set; } = null!;
        public SeatType SeatType { get; set; }

        public IList<EventSeat> EventSeats { get; set; } = [];
    }
}