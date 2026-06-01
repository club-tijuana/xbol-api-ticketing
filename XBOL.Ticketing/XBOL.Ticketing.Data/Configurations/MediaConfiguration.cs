using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Configurations
{
    public class MediaConfiguration : IEntityTypeConfiguration<Media>
    {
        public void Configure(EntityTypeBuilder<Media> builder)
        {
            builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId, x.MediaType, x.Order })
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL")
                .HasDatabaseName("UX_Media_ActiveReferenceTypeReferenceIdMediaTypeOrder");

            builder.HasOne(x => x.BlobAsset)
                .WithMany()
                .HasForeignKey(x => x.BlobAssetId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
