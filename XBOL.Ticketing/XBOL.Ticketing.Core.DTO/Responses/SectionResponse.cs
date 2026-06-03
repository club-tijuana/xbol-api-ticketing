namespace XBOL.Ticketing.Core.DTO.Responses
{
    public class SectionResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public long? PriceListItemId { get; set; }
    }
}
