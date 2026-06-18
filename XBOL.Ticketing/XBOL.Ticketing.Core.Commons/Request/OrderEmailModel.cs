namespace XBOL.Ticketing.Core.Commons.Request
{
    public class OrderEmailModel : EmailModelBase
    {
        public string EventTitle { get; set; }
        public string EventSubtitle { get; set; }
        public string EventImageUrl { get; set; }
        public OrderDetailsInfo OrderDetails { get; set; }
        public OrderClientInfo ClientInfo { get; set; }
        public List<SeatInfo> Seats { get; set; }
        public string GoogleWalletUrl { get; set; }
        public string AppleWalletUrl { get; set; }
        public List<string> EntryInstructions { get; set; }
        public string? PromoBannerImageUrl { get; set; }
        public string? PromoBannerLinkUrl { get; set; }
        public string? LogoImageUrl { get; set; }
        public bool IsBundle { get; set; } = false;
    }

    public class OrderDetailsInfo
    {
        public string OrderNumber { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string SubTotal { get; set; }
        public string Total { get; set; }
        public VenueInfo Venue { get; set; }
        public List<OrderFeeInfo> Fees { get; set; }
        public string PurchaseDate { get; set; }
        public string PaymentMethod { get; set; }
    }

    public class OrderClientInfo
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class VenueInfo
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public class OrderFeeInfo
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }

    public class SeatInfo
    {
        public string TicketType { get; set; }
        public string SeatKey { get; set; }
        public string Zone { get; set; }
        public string Row { get; set; }
        public string Seat { get; set; }
        public decimal Price { get; set; }
        public List<SeatFeeInfo> Fees { get; set; }
    }

    public class SeatFeeInfo
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}
