namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class PaymentLinkRequest
    {
        public long? ExpirationDays { get; set; }
        public long? ExpirationHours { get; set; }
        public long? ExpirationMinutes { get; set; }
    }
}
