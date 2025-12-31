namespace XBOL.Ticketing.Core.Model
{
    public class OrderTax : BaseModel
    {
        public long OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public string TaxType { get; set; } = null!;
        public decimal Amount { get; set; }
    }
}