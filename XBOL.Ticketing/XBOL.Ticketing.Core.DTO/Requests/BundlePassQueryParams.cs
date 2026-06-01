using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class BundlePassQueryParams
    {
        public long? BundleId { get; set; }
        public BundlePassStatus? Status { get; set; }
        public string SearchTerm { get; set; } = "";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
