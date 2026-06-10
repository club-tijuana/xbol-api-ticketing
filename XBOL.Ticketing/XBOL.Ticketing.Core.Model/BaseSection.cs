using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class BaseSection : BaseModel
    {
        public long BaseZoneId { get; set; }
        public BaseZone BaseZone { get; set; } = null!;

        public string Name { get; set; } = null!;
        public string? DisplayName { get; set; }
        public SectionType SectionType { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        public IList<BaseRow> BaseRows { get; set; } = [];
        public IList<GateAccessRule> GateAccessRules { get; set; } = [];
    }
}
