using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Configurations
{
    public class PriceReferenceConfiguration : IEntityTypeConfiguration<PriceReference>
    {
        public void Configure(EntityTypeBuilder<PriceReference> builder)
        {
            builder.HasMany(p => p.PriceSegments);
        }
    }
}
