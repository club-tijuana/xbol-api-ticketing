namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class PaymentInfoRequest
    {
        public bool IsCourtesy { get; set; }
        public decimal CardAmount { get; set; }
        public decimal CashAmount { get; set; }
        public decimal DolarAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal OtherAmount { get; set; }
    }
}
