using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.Model
{
    public class AdditionalCharge : BaseModel
    {
        public long PriceReferenceId { get; set; }
        public PriceReference PriceReference { get; set; } = null!;
        public string Name { get; set; } = null!;
        public FeeType FeeType { get; set; }
        public decimal Value { get; set; }
        public bool AppliesToTotal { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }
    }
}
