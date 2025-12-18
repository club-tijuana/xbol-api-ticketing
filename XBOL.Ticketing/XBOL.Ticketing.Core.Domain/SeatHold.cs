using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class SeatHold
    {
        public long Id { get; set; }

        public long EventSeatId { get; set; }
        public EventSeat EventSeat { get; set; } = null!;

        public long? ClientId { get; set; }
        public Client? Client { get; set; }

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public SeatHoldSource HoldSource { get; set; }
        public string ExternalReference { get; set; } = null!;

        public DateTimeOffset ExpiresAt { get; set; }
        public SeatHoldStatus Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}