using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class EventScheduleRequest
    {
        public long EventId { get; set; }
        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset EndDateTime { get; set; }
        public DateTimeOffset? PublishedDate { get; set; }
        public DateTimeOffset? PreSaleStartDate { get; set; }
        public DateTimeOffset? PreSaleEndDate { get; set; }
        public DateTimeOffset OnSaleDate { get; set; }
        public DateTimeOffset OffSaleDate { get; set; }
        public DateTimeOffset? GateOpenDate { get; set; }
        public GameCategory? GameCategory { get; set; }
        public int? HoldExpirationInMinutes { get; set; }
    }
}
