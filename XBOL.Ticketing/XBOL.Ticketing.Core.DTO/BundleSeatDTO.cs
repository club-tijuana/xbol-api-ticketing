namespace XBOL.Ticketing.Core.DTO
{
    public class BundleSeatDTO
    {
        public long Id { get; set; }
        public long BundleSectionId { get; set; }
        public long BaseSeatId { get; set; }
        public decimal? PriceOverride { get; set; }
        public string ExternalSeatObjectKey { get; set; } = null!;
        public bool ForSale { get; set; }
    }
}
