namespace XBOL.Ticketing.Core.DTO.Responses
{
    public class SectionPriceResponse
    {
        public List<string> Objects { get; set; } = new List<string>();
        public decimal? Price { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
