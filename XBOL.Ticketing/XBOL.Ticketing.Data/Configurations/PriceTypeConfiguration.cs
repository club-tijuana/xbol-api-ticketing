using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Configurations
{
    public class PriceTypeConfiguration : IEntityTypeConfiguration<PriceType>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PriceType> builder)
        {
            builder.Property(pt => pt.Name).IsRequired().HasMaxLength(100);
            builder.Property(pt => pt.Label).HasMaxLength(100);
            builder.Property(pt => pt.Description).HasMaxLength(500);
        }
    }
}
