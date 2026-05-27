namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class BundleQueryParams
    {
        public string SearchTerm { get; set; } = "";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
