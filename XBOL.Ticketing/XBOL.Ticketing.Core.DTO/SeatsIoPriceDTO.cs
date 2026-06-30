namespace XBOL.Ticketing.Core.DTO
{
    public class SeatsIoPriceDTO
    {
        public long? PriceListItemId { get; set; }
        public decimal? Price { get; set; }
        public long? BasePriceListItemId { get; set; }
        public decimal? BasePrice { get; set; }
        public string[]? Objects { get; set; }
        public long? Category { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? Fee { get; set; }
        public List<SeatFeeDTO> Fees { get; set; } = [];
        public TicketTypeDTO[]? TicketTypes { get; set; }
    }

    public class SeatFeeDTO
    {
        public string FeeName { get; set; } = string.Empty;
        public string FeeType { get; set; } = string.Empty;
        public string ChargeCategory { get; set; } = "Fee";
        public decimal FeeAmount { get; set; }
    }

    public class TicketTypeDTO
    {
        public long PriceListItemId { get; set; }

        public string TicketType { get; set; } = "";
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? Fee { get; set; }
        public List<SeatFeeDTO> Fees { get; set; } = [];
        public string? Label { get; set; }
        public string? Description { get; set; }

        public bool? Primary { get; set; }
        public bool? Unavailable { get; set; }
        public string? UnavailableReason { get; set; }
    }
}
