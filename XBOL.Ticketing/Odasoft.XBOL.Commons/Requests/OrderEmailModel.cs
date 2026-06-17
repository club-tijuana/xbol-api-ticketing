namespace Odasoft.XBOL.Commons.Requests;

public class OrderEmailModel : EmailModelBase
{
    public required string EventTitle { get; set; }
    public required string EventSubtitle { get; set; }
    public required string EventImageUrl { get; set; }
    public required OrderDetailsInfo OrderDetails { get; set; }
    public required OrderClientInfo ClientInfo { get; set; }
    public required List<SeatInfo> Seats { get; set; }
    public required string GoogleWalletUrl { get; set; }
    public required string AppleWalletUrl { get; set; }
    public required List<string> EntryInstructions { get; set; }
    public string? PromoBannerImageUrl { get; set; }
    public string? PromoBannerLinkUrl { get; set; }
    public string? LogoImageUrl { get; set; }
}

public class OrderDetailsInfo
{
    public required string OrderNumber { get; set; }
    public required string Date { get; set; }
    public required string Time { get; set; }
    public required string SubTotal { get; set; }
    public required string Total { get; set; }
    public required VenueInfo Venue { get; set; }
    public required List<OrderFeeInfo> Fees { get; set; }
    public required string PurchaseDate { get; set; }
    public required string PaymentMethod { get; set; }
}

public class OrderClientInfo
{
    public required string Name { get; set; }
    public required string Email { get; set; }
}

public class VenueInfo
{
    public required string Name { get; set; }
    public required string Address { get; set; }
}

public class OrderFeeInfo
{
    public required string Description { get; set; }
    public required decimal Amount { get; set; }
}

public class SeatInfo
{
    public required string TicketType { get; set; }
    public required string SeatKey { get; set; }
    public required string Zone { get; set; }
    public required string Row { get; set; }
    public required string Seat { get; set; }
    public required decimal Price { get; set; }
    public required List<SeatFeeInfo> Fees { get; set; }
}

public class SeatFeeInfo
{
    public required string Description { get; set; }
    public required decimal Amount { get; set; }
}
