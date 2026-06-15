using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class HoldSeatsRequest
    {
        public required long Id { get; set; }
        public required List<string> Seats { get; set; }
        public required SaleType SaleType { get; set; }
    }
}
