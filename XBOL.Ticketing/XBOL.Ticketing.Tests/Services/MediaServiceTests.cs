using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Repositories.Media;
using XBOL.Ticketing.Services.Media;

namespace XBOL.Ticketing.Tests.Services;

public class MediaServiceTests
{
    [Fact]
    public async Task GetProductMediaAsync_ReturnsAvailableBlobAssetMetadata()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = Options(connection);
        await using (var context = new XBOLDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Media.AddRange(
                CreateMedia(10, MediaType.Banner, 0, BlobAssetStatus.Available, "banner.png", "https://cdn.test/banner.png"),
                CreateMedia(10, MediaType.Gallery, 1, BlobAssetStatus.PendingUpload, "pending.png", null),
                CreateMedia(10, MediaType.Logo, 2, BlobAssetStatus.Available, "deleted.png", "https://cdn.test/deleted.png", DateTimeOffset.UtcNow),
                CreateMedia(11, MediaType.Banner, 0, BlobAssetStatus.Available, "other.png", "https://cdn.test/other.png"));
            await context.SaveChangesAsync();
        }

        await using var readContext = new XBOLDbContext(options);
        var service = new MediaService(new MediaRepository(readContext));

        var result = await service.GetProductMediaAsync(10, SaleType.Bundle);

        result.Should().ContainSingle();
        result[0].FileName.Should().Be("banner.png");
        result[0].Title.Should().Be("banner.png");
        result[0].ContentType.Should().Be("image/png");
        result[0].Url.Should().Be("https://cdn.test/banner.png");
        result[0].MediaType.Should().Be(MediaType.Banner);
        result[0].Order.Should().Be(0);
    }

    [Fact]
    public async Task GetProductMediaByMediaTypeAsync_FiltersAvailableBlobAssetMediaByType()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = Options(connection);
        await using (var context = new XBOLDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Media.AddRange(
                CreateMedia(10, MediaType.Banner, 0, BlobAssetStatus.Available, "banner.png", "https://cdn.test/banner.png"),
                CreateMedia(10, MediaType.Gallery, 0, BlobAssetStatus.Available, "gallery.png", "https://cdn.test/gallery.png"));
            await context.SaveChangesAsync();
        }

        await using var readContext = new XBOLDbContext(options);
        var service = new MediaService(new MediaRepository(readContext));

        var result = await service.GetProductMediaByMediaTypeAsync(10, SaleType.Bundle, MediaType.Gallery);

        result.Should().ContainSingle();
        result[0].MediaType.Should().Be(MediaType.Gallery);
        result[0].Url.Should().Be("https://cdn.test/gallery.png");
    }

    private static Media CreateMedia(
        long referenceId,
        MediaType mediaType,
        int order,
        BlobAssetStatus status,
        string fileName,
        string? url,
        DateTimeOffset? deletedAt = null)
    {
        var now = DateTimeOffset.UtcNow;

        return new Media
        {
            ReferenceId = referenceId,
            ReferenceType = SaleType.Bundle,
            MediaType = mediaType,
            Order = order,
            CreatedAt = now,
            UpdatedAt = now,
            BlobAsset = new BlobAsset
            {
                BucketName = "bucket",
                ObjectName = $"media/{fileName}",
                FileName = fileName,
                ContentType = "image/png",
                SizeBytes = 42,
                Url = url,
                Status = status,
                CreatedAt = now,
                UpdatedAt = now
            },
            DeletedAt = deletedAt
        };
    }

    private static DbContextOptions<XBOLDbContext> Options(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;
    }
}
