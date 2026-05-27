namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class BookingSeatRequest
    {
        public required string SeatKey { get; set; }
        public required decimal SeatPrice { get; set; }
        public required long PriceListItemId { get; set; }
    }
}
