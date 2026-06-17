namespace XBOL.Ticketing.Core.DTO.EvoPayment
{
    public class CheckoutSeatRequest
    {
        public required string SeatKey { get; init; }
        public long PriceListItemId { get; init; }
    }
}
