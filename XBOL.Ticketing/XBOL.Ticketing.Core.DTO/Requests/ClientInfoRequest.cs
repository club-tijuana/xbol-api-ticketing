namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class ClientInfoRequest
    {
        public long? Id { get; set; }
        public string CountryPhoneISO { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }
}
