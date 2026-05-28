namespace XBOL.Ticketing.Core.DTO.Results
{
    public class EventCategoryResult
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
