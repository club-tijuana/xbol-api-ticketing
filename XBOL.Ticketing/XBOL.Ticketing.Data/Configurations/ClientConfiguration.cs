using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Extensions;

namespace XBOL.Ticketing.Data.Configurations
{
    public class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            builder.Property(x => x.PhoneNumber)
                    .IsRequired()
                    .HasMaxLength(15)
                    .HasConversion<PhoneSanitizerConverter>();

            builder.Property(x => x.PhoneRegionCodeId)
                    .IsRequired();

            builder.HasIndex(x => new { x.PhoneRegionCodeId, x.PhoneNumber })
                    .IsUnique();
        }
    }
}
