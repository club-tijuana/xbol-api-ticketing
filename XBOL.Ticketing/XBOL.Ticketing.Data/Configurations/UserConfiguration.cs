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

            builder.HasOne(x => x.Client)
                   .WithOne(x => x.User)
                   .HasForeignKey<Client>(x => x.UserId);

            builder.HasOne(x => x.OrganizerMember)
                   .WithOne(x => x.User)
                   .HasForeignKey<OrganizerMember>(x => x.UserId);
        }
    }
}
