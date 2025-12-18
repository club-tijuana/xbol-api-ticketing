using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class BaseSection
    {
        public long Id { get; set; }

        public long BaseZoneId { get; set; }
        public BaseZone BaseZone { get; set; } = null!;

        public string Name { get; set; } = null!;
        public SectionType SectionType { get; set; }

        public IList<BaseRow> BaseRows { get; set; } = [];
        public IList<GateAccessRule> GateAccessRules { get; set; } = [];
    }
}