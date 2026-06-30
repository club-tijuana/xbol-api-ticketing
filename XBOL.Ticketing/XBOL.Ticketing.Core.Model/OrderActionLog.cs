using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class OrderActionLog : BaseModel
    {
        public long OrderId { get; set; }

        public Order Order { get; set; } = null!;

        public string Seats { get; set; } = "";

        public OrderAction Action { get; set; }

        public string ActionName { get; set; } = "";

        public string Comments { get; set; } = "";

        public DateTimeOffset CreatedAt { get; set; }

        public Guid CreatedBy { get; set; }
    }
}
