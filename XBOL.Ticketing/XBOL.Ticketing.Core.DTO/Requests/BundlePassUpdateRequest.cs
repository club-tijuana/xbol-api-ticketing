using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class BundlePassUpdateRequest
    {
        public BundlePassStatus? Status { get; set; }
        public BundlePassSuspendedReason? SuspendedReason { get; set; }
        public string? SuspendedOtherReason { get; set; }
        public decimal? Price { get; set; }
        public long? BundleSeatId { get; set; }
        public bool? IsDigital { get; set; }
    }
}
