namespace XBOL.Ticketing.Core.Model
{
    public class AccreditationType
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;

        public IList<Accreditation> Accreditations { get; set; } = [];
    }
}