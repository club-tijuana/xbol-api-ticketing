using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Configurations
{
    public class BundleEventScheduleConfiguration : IEntityTypeConfiguration<BundleEventSchedule>
    {
        public void Configure(EntityTypeBuilder<BundleEventSchedule> builder)
        {
            builder.HasKey(bes => new { bes.BundleId, bes.EventScheduleId });
        }
    }
}
