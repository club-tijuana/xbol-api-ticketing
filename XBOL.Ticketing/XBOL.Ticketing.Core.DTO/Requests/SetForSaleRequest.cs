namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class SetForSaleRequest
    {
        public required string EventKey { get; set; }
        public required List<string> SeatKeys { get; set; }
        public required bool ForSale { get; set; }
    }
}
