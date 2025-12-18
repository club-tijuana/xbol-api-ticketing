namespace XBOL.Ticketing.Core.Model
{
    public class Accreditation
    {
        public long Id { get; set; }

        public long AccreditationTypeId { get; set; }
        public AccreditationType AccreditationType { get; set; } = null!;

        public DateTimeOffset ValidForDate { get; set; }
        public string Notes { get; set; } = null!;
    }
}