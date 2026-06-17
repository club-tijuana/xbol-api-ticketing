using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class PaymentChangeDetail : BaseModel
    {
        public long OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public CurrencyType Currency { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountMXN { get; set; }
        public decimal ExchangeRate { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
    }
}
