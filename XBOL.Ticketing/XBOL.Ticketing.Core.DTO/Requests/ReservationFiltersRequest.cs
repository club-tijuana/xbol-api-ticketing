namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class ReservationFiltersRequest
    {
        public long? SeasonId { get; set; }
        public long? ScheduleId { get; set; }
        public long? SectionId { get; set; }
        public long? ZoneId { get; set; }
        public decimal? MinimumPrice { get; set; }
        public decimal? MaximumPrice { get; set; }
    }
}
