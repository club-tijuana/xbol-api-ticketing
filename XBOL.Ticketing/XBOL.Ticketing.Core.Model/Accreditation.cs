namespace XBOL.Ticketing.Core.Model
{
    public class Accreditation : BaseModel
    {
        public long AccreditationTypeId { get; set; }
        public AccreditationType AccreditationType { get; set; } = null!;

        public DateTimeOffset ValidForDate { get; set; }
        public string Notes { get; set; } = null!;
    }
}