namespace XBOL.Ticketing.Core.DTO
{
    public class PaymentInfoRequest
    {
        public decimal? CardAmount { get; set; }
        public decimal? CashAmount { get; set; }
        public decimal? DolarAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal? OtherAmount { get; set; }
    }
}
