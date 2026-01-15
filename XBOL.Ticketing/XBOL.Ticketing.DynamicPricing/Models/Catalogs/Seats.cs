namespace XBOL.Ticketing.DynamicPricing.Models.Catalogs
{
    public class Seat
    {
        public long SeatId { get; set; }
        public string SeatCode { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
    }
}
