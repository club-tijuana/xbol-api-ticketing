using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class InventoryBatch
    {
        public long Id { get; set; }

        public long EventScheduleId { get; set; }
        public EventSchedule EventSchedule { get; set; } = null!;

        public long DistributorId { get; set; }
        public Distributor Distributor { get; set; } = null!;

        public int Quantity { get; set; }
        public DateTimeOffset CutoffDate { get; set; }

        public InventoryBatchStatus Status { get; set; }

        public IList<Ticket> Tickets { get; set; } = [];
    }
}