using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Extensions;

namespace XBOL.Ticketing.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(x => x.PhoneNumber)
                    .HasMaxLength(15)
                    .HasConversion<PhoneSanitizerConverter>();

            builder.HasIndex(x => new { x.PhoneRegionCodeId, x.PhoneNumber })
                    .IsUnique()
                    .HasFilter("\"PhoneNumber\" IS NOT NULL AND \"PhoneNumber\" <> ''");

            builder.HasOne(x => x.Organizer)
                   .WithMany(x => x.OrganizerMembers)
                   .HasForeignKey(x => x.OrganizerId);
        }
    }
}
