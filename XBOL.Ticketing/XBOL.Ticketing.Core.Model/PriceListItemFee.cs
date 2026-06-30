namespace XBOL.Ticketing.Core.Model
{
    public class PriceListItemFee : BaseModel
    {
        public long PriceListItemId { get; set; }
        public PriceListItem PriceListItem { get; set; } = null!;
        public long AdditionalChargeId { get; set; }
        public AdditionalCharge AdditionalCharge { get; set; } = null!;
        public string FeeName { get; set; } = null!;
        public string FeeType { get; set; } = null!;
        public string ChargeCategory { get; set; } = "Fee";
        public decimal FeeValue { get; set; }
        public decimal FeeAmount { get; set; }
    }
}
