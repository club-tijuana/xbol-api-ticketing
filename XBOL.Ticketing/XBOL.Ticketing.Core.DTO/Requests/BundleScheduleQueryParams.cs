namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class BundleScheduleQueryParams
    {
        public string SearchTerm { get; set; } = "";
        public string? Venue { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string? SortBy { get; set; }
        public bool Descending { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
