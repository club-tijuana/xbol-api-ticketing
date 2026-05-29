using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(x => x.PhoneNumber).HasMaxLength(15);

            builder.HasOne(x => x.Organizer)
                   .WithMany(x => x.OrganizerMembers)
                   .HasForeignKey(x => x.OrganizerId);
        }
    }
}
