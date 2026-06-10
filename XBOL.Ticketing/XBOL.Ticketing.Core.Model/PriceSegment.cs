using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class PriceSegment : BaseModel
    {
        public long PriceReferenceId { get; set; }
        public PriceReference PriceReference { get; set; } = null!;
        public long BaseZoneId { get; set; }
        public BaseZone BaseZone { get; set; } = null!;
        public long? BaseSectionId { get; set; }
        public BaseSection? BaseSection { get; set; }
        public long? BaseRowId { get; set; }
        public BaseRow? BaseRow { get; set; }
        public long? BaseSeatId { get; set; }
        public BaseSeat? BaseSeat { get; set; }
        public PriceItemType PriceItemType { get; set; }
        public long VenueMapId { get; set; }
        public VenueMap VenueMap { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public List<Price> Prices { get; set; } = [];
        public List<PriceType> PriceTypes { get; set; } = [];
    }
}
