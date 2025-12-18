using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class OrderItem
    {
        public long Id { get; set; }

        public long OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public ItemType ItemType { get; set; }
        public long ItemReferenceId { get; set; }

        public decimal Price { get; set; }
    }
}