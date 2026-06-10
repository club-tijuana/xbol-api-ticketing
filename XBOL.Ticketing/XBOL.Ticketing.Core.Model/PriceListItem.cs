namespace XBOL.Ticketing.Core.Model
{
    public class PriceListItem : BaseModel
    {
        public long PriceListId { get; set; }
        public PriceList PriceList { get; set; } = null!;
        public long? BaseZoneId { get; set; }
        public BaseZone? BaseZone { get; set; }
        public long? BaseSectionId { get; set; }
        public BaseSection? BaseSection { get; set; }
        public long? BaseRowId { get; set; }
        public BaseRow? BaseRow { get; set; }
        public long? BaseSeatId { get; set; }
        public BaseSeat? BaseSeat { get; set; }
        public long PriceId { get; set; }
        public Price Price { get; set; } = null!;
        public long PriceTypeId { get; set; }
        public PriceType PriceType { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public decimal FinalPrice { get; set; }

        public List<PriceListItemFee> FeeList { get; set; } = [];
    }
}
