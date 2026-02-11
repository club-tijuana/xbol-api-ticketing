namespace XBOL.Ticketing.Core.DTO
{
    public class EventClientContactRequest
    {
        public required string CountryPhoneISO { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Email { get; set; }
        public string? FullName { get; set; }
    }
}
