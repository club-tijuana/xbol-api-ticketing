namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class AuthenticationRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public long OrderId { get; set; }
        public string OrderRefId { get; set; } = string.Empty;
        public string TransactionRefId { get; set; } = string.Empty;
    }
}
