using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Configurations
{
    public class PriceSegmentConfiguration : IEntityTypeConfiguration<PriceSegment>
    {
        public void Configure(EntityTypeBuilder<PriceSegment> builder)
        {
            builder.HasMany(p => p.Prices);
        }
    }
}
