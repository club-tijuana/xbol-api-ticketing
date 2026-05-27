using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Configurations
{
    public class EventScheduleConfiguration : IEntityTypeConfiguration<EventSchedule>
    {
        public void Configure(EntityTypeBuilder<EventSchedule> builder)
        {
            builder.HasIndex(e => e.ExternalEventKey);

            builder.HasQueryFilter(e => e.DeletedAt == null);
        }
    }
}
