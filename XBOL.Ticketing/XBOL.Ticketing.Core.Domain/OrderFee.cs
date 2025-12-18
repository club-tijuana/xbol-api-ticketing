namespace XBOL.Ticketing.Core.Model
{
    public class OrderFee
    {
        public long Id { get; set; }

        public long OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public string FeeType { get; set; } = null!;
        public decimal Amount { get; set; }
    }
}