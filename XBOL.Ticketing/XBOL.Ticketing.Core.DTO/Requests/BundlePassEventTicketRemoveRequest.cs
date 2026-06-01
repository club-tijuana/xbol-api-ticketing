namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class BundlePassEventTicketRemoveRequest
    {
        public required List<long> TicketIds { get; set; }
    }
}
