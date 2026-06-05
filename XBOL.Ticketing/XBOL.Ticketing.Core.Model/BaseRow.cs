namespace XBOL.Ticketing.Core.Model
{
    public class BaseRow : BaseModel
    {
        public long BaseSectionId { get; set; }
        public BaseSection BaseSection { get; set; } = null!;

        public string RowLabel { get; set; } = null!;

        public string? DisplayName { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        public IList<BaseSeat> BaseSeats { get; set; } = [];
    }
}
