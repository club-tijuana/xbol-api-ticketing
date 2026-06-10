namespace XBOL.Ticketing.Core.DTO.Responses
{
    public class SeatResponse
    {
        public long Id { get; set; }
        public string ExternalSeatObjectKey { get; set; } = string.Empty;
        public decimal? PriceOverride { get; set; }
        public long? PriceListItemId { get; set; }
    }
}
