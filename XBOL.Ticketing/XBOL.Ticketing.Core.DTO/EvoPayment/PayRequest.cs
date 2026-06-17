namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class PayRequest
    {
        public long OrderId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public Guid OrderRefId { get; set; }
        public Guid TransactionRefId { get; set; }
    }
}
