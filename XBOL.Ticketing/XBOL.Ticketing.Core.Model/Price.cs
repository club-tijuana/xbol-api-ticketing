namespace XBOL.Ticketing.Core.Model
{
    public class Price : BaseModel
    {
        public long PriceSegmentId { get; set; }
        public PriceSegment PriceSegment { get; set; } = null!;
        public long PriceTypeId { get; set; }
        public PriceType PriceType { get; set; } = null!;
        public decimal PriceValue { get; set; }
        public bool ApplyDynamicPricing { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
