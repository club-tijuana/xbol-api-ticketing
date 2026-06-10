using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class BundlePassCreateRequest
    {
        public required long BundleId { get; set; }
        public long? ClientId { get; set; }
        public Guid? UserId { get; set; }
        public long? BundleSeatId { get; set; }
        public required BundlePassType BundlePassType { get; set; }
        public bool IsDigital { get; set; } = true;
        public required decimal Price { get; set; }
    }
}
