using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class BaseSeat : BaseModel
    {
        public long BaseRowId { get; set; }
        public BaseRow BaseRow { get; set; } = null!;

        public string? DisplayName { get; set; }

        public string SeatNumber { get; set; } = null!;
        public SeatType SeatType { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        public IList<EventSeat> EventSeats { get; set; } = [];
    }
}
