namespace XBOL.Ticketing.Core.Model
{
    public class SequenceTracker : BaseModel
    {
        public string SequenceKey { get; set; } = "";
        public long LastValue { get; set; }
    }
}
