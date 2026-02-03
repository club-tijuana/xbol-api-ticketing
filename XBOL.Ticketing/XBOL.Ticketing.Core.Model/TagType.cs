namespace XBOL.Ticketing.Core.Model
{
    public class TagType : BaseModel
    {
        public string Name { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        public IList<Tag> Tags { get; set; } = [];
    }
}