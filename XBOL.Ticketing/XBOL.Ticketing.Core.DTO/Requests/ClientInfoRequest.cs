using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Core.DTO.Requests
{
    public class ClientInfoRequest
    {
        public long? Id { get; set; }
        public required long PhoneRegionCodeId { get; set; }
        public required string PhoneNumber { get; set; }
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? City { get; set; }
        public string? Neighborhood { get; set; }
        public ClientGender? Gender { get; set; }
        public DateTimeOffset? Birthday { get; set; }
    }
}
