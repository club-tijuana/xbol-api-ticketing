using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class Media : BaseModel
    {
        public SaleType ReferenceType { get; set; }
        public long ReferenceId { get; set; } = 0;
        public MediaType MediaType { get; set; }
        public byte[] Content { get; set; } = [];
        public string ContentType { get; set; } = "";
        public string FileName { get; set; } = "";
        public string? Url { get; set; }
        public int Order { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
