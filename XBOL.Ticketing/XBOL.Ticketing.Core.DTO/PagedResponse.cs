namespace XBOL.Ticketing.Core.DTO
{
    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = [];
        public required int TotalCount { get; set; }
        public required int Page { get; set; }
        public required int PageSize { get; set; }
    }
}
