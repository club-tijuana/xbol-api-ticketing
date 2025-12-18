using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class PriceRule
    {
        public long Id { get; set; }

        public PriceRuleScope Scope { get; set; }

        public long? EventSectionId { get; set; }
        public EventSection? EventSection { get; set; }

        public long? BaseRowId { get; set; }
        public BaseRow? BaseRow { get; set; }

        public long? BaseSeatId { get; set; }
        public BaseSeat? BaseSeat { get; set; }

        public long? EventScheduleId { get; set; }
        public EventSchedule? EventSchedule { get; set; }

        public string Expression { get; set; } = null!;

        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public int Priority { get; set; }
    }
}