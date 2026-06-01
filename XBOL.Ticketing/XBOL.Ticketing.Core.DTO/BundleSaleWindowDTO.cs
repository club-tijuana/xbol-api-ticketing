namespace XBOL.Ticketing.Core.DTO
{
    public class BundleSaleWindowDTO
    {
        public required string BundleScheduleKey { get; set; }
        public long BundleId { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public DateTimeOffset? PublishedDate { get; set; }
        public DateTimeOffset? OnSaleDate { get; set; }
        public DateTimeOffset? PreSaleDate { get; set; }
        public DateTimeOffset? OffSaleDate { get; set; }
        public DateTimeOffset? RenewalStartDate { get; set; }
        public DateTimeOffset? RenewalEndDate { get; set; }
        public string? ExternalKey { get; set; }
    }
}
