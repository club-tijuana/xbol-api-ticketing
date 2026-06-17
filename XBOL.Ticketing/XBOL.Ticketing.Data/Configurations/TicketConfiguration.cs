using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Configurations
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.Property(t => t.PrivateToken).IsRequired(false);

            builder.HasOne(a => a.OriginalClient)
                   .WithMany(b => b.Tickets)
                   .HasForeignKey(b => b.OriginalClientId);

            builder.HasOne(a => a.CurrentClient)
                   .WithMany()
                   .HasForeignKey(b => b.CurrentClientId);

            builder.HasOne(x => x.SeasonPassEventTicket)
                   .WithOne(x => x.Ticket)
                   .HasForeignKey<SeasonPassEventTicket>(x => x.TicketId);

            builder.HasOne(x => x.BundlePassEventTicket)
                   .WithOne(x => x.Ticket)
                   .HasForeignKey<BundlePassEventTicket>(x => x.TicketId);
        }
    }
}
