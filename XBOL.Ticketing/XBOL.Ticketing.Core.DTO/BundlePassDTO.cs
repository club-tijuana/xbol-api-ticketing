using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO
{
    public class BundlePassDTO
    {
        public long Id { get; set; }
        public long BundleId { get; set; }
        public long? ClientId { get; set; }
        public Guid? UserId { get; set; }
        public long? BundleSeatId { get; set; }
        public string TrackingCode { get; set; } = null!;
        public string PrivateToken { get; set; } = null!;
        public BundlePassType BundlePassType { get; set; }
        public BundlePassStatus Status { get; set; }
        public BundlePassSuspendedReason? SuspendedReason { get; set; }
        public string? SuspendedOtherReason { get; set; }
        public bool IsDigital { get; set; }
        public decimal Price { get; set; }
        public DateTimeOffset PurchasedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
