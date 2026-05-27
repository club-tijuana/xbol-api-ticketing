using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.UseTpcMappingStrategy();

            builder.HasMany(e => e.Categories)
                .WithMany(c => c.Events)
                .UsingEntity(j => j.ToTable("EventEventCategory"));
        }
    }
}
