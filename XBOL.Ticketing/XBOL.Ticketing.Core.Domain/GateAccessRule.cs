namespace XBOL.Ticketing.Core.Model
{
    public class GateAccessRule
    {
        public long Id { get; set; }

        public long GateId { get; set; }
        public Gate Gate { get; set; } = null!;

        public long BaseSectionId { get; set; }
        public BaseSection BaseSection { get; set; } = null!;
    }
}