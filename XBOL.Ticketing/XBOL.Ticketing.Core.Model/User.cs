using Microsoft.AspNetCore.Identity;

namespace XBOL.Ticketing.Core.Model
{
    public class User : IdentityUser<Guid>
    {
        public long? ClientId { get; set; }
        public Client? Client { get; set; }

        public long? OrganizerMemberId { get; set; }
        public OrganizerMember? OrganizerMember { get; set; }

        public string EmailNormalized { get; set; } = null!;

        public string CountryPhoneCode { get; set; } = null!;
        public string CountryPhoneISO { get; set; } = null!;
        public string PhoneNumberNormalized { get; set; } = null!;

        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }

        public DateTimeOffset? EmailVerifiedTimeStamp { get; set; }
        public DateTimeOffset? PhoneVerifiedTimeStamp { get; set; }

        public bool IsActive { get; set; }
        public bool IsMfaEnabled { get; set; }
        public string? MfaMethod { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public Guid CreatedBy { get; set; }
        public Guid UpdatedBy { get; set; }

        public DateTimeOffset? LastLogin { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTimeOffset? LockedUntil { get; set; }

        public IList<Order> Orders { get; set; } = [];
        public IList<PromoCodeRedemption> PromoCodeRedemptions { get; set; } = [];
        public IList<SeatHold> SeatHolds { get; set; } = [];
        public IList<AuditLog> AuditLogs { get; set; } = [];
        public IList<Ticket> Tickets { get; set; } = [];
    }
}