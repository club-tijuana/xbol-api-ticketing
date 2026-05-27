using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Configurations
{
    public class BundlePassConfiguration : IEntityTypeConfiguration<BundlePass>
    {
        public void Configure(EntityTypeBuilder<BundlePass> builder)
        {
            builder.HasMany(bp => bp.BundlePassEventTickets)
                .WithOne(bpet => bpet.BundlePass)
                .HasForeignKey(bpet => bpet.BundlePassId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(bp => bp.BundleSeat)
            .WithMany(bs => bs.BundlePasses) // TODO: This should be WithOne if a BundleSeat can only be associated with one BundlePass
            .HasForeignKey(bp => bp.BundleSeatId)
            .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(bp => bp.Client)
                .WithMany()
                .HasForeignKey(bp => bp.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(bp => bp.User)
                .WithMany()
                .HasForeignKey(bp => bp.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
