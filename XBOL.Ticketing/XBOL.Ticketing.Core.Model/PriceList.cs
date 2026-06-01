using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class PriceList : BaseModel
    {
        public long PriceReferenceId { get; set; }
        public PriceReference PriceReference { get; set; } = null!;
        public int VersionNumber { get; set; } = 1; // Start with version 1
        public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
        public Guid PublishBy { get; set; } = Guid.Empty;
        public VersionStatus Status { get; set; }

        public List<PriceListItem> Items { get; set; } = [];
    }
}
