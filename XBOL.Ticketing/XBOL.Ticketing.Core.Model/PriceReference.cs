using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class PriceReference : BaseModel
    {
        public SaleType ReferenceType { get; set; }
        public long ReferenceId { get; set; }
        public bool IsActive { get; set; }

        public List<PriceSegment> PriceSegments { get; set; } = [];
    }
}
