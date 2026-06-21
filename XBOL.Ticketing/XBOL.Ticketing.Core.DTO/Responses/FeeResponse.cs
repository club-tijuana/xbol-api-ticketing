namespace XBOL.Ticketing.Core.DTO.Responses
{
    public class FeeResponse
    {
        public string FeeName { get; set; } = string.Empty;
        public decimal FeeAmount { get; set; }
        public string ChargeCategory { get; set; } = "Fee";
    }
}
