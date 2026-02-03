namespace XBOL.Ticketing.Core.Model
{
    public class AccreditationType : BaseModel
    {
        public string Name { get; set; } = null!;

        public IList<Accreditation> Accreditations { get; set; } = [];
    }
}