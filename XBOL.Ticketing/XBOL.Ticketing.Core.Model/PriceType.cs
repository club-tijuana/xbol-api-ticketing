namespace XBOL.Ticketing.Core.Model
{
    public class PriceType : BaseModel
    {
        public long PriceSegmentId { get; set; }
        public PriceSegment PriceSegment { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Label { get; set; }
        public string? Description { get; set; }
        public bool IsBasePrice { get; set; } = false;
        public bool Primary { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
