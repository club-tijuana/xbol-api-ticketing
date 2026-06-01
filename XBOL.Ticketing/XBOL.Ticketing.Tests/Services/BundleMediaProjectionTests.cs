using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Data.Repositories.Bundle;
using XBOL.Ticketing.Data.Repositories.Media;
using XBOL.Ticketing.Services.Bundle;
using XBOL.Ticketing.Services.Media;

namespace XBOL.Ticketing.Tests.Services;

public class BundleMediaProjectionTests
{
    [Fact]
    public async Task GetByIdAsync_UsesBlobAssetUrlsForBannerAndPoster()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = Options(connection);
        long bundleId;
        await using (var context = new XBOLDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var bundle = new Bundle
            {
                Name = "Bundle",
                BundleType = BundleType.Basic,
                BundlePricingType = BundlePricingType.Single,
                Status = EventStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            context.Bundles.Add(bundle);
            await context.SaveChangesAsync();
            bundleId = bundle.Id;

            context.Media.AddRange(
                CreateMedia(bundleId, MediaType.Banner, 0, "banner.png", "https://cdn.test/banner.png"),
                CreateMedia(bundleId, MediaType.Logo, 0, "poster.png", "https://cdn.test/poster.png"));
            await context.SaveChangesAsync();
        }

        await using var readContext = new XBOLDbContext(options);
        var service = CreateBundleService(readContext);

        var result = await service.GetByIdAsync(bundleId);

        result.Should().NotBeNull();
        result!.BannerImageUrl.Should().Be("https://cdn.test/banner.png");
        result.PosterImageUrl.Should().Be("https://cdn.test/poster.png");
        result.Media.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_UsesLowestOrderBlobAssetUrlsForBannerAndPoster()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = Options(connection);
        long bundleId;
        await using (var context = new XBOLDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var bundle = new Bundle
            {
                Name = "Bundle",
                BundleType = BundleType.Basic,
                BundlePricingType = BundlePricingType.Single,
                Status = EventStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            context.Bundles.Add(bundle);
            await context.SaveChangesAsync();
            bundleId = bundle.Id;

            context.Media.AddRange(
                CreateMedia(bundleId, MediaType.Banner, 1, "secondary-banner.png", "https://cdn.test/secondary-banner.png"),
                CreateMedia(bundleId, MediaType.Banner, 0, "primary-banner.png", "https://cdn.test/primary-banner.png"),
                CreateMedia(bundleId, MediaType.Logo, 1, "secondary-poster.png", "https://cdn.test/secondary-poster.png"),
                CreateMedia(bundleId, MediaType.Logo, 0, "primary-poster.png", "https://cdn.test/primary-poster.png"));
            await context.SaveChangesAsync();
        }

        await using var readContext = new XBOLDbContext(options);
        var service = CreateBundleService(readContext);

        var result = await service.GetByIdAsync(bundleId);

        result.Should().NotBeNull();
        result!.BannerImageUrl.Should().Be("https://cdn.test/primary-banner.png");
        result.PosterImageUrl.Should().Be("https://cdn.test/primary-poster.png");
    }

    [Fact]
    public async Task GetByIdAsync_KeepsExistingImageUrlsWhenBlobUrlsAreMissing()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = Options(connection);
        long bundleId;
        await using (var context = new XBOLDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var bundle = new Bundle
            {
                Name = "Bundle",
                BundleType = BundleType.Basic,
                BundlePricingType = BundlePricingType.Single,
                Status = EventStatus.Draft,
                BannerImageUrl = "https://legacy.test/banner.png",
                PosterImageUrl = "https://legacy.test/poster.png",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            context.Bundles.Add(bundle);
            await context.SaveChangesAsync();
            bundleId = bundle.Id;

            context.Media.AddRange(
                CreateMedia(bundleId, MediaType.Banner, 0, "banner.png", null),
                CreateMedia(bundleId, MediaType.Logo, 0, "poster.png", null));
            await context.SaveChangesAsync();
        }

        await using var readContext = new XBOLDbContext(options);
        var service = CreateBundleService(readContext);

        var result = await service.GetByIdAsync(bundleId);

        result.Should().NotBeNull();
        result!.BannerImageUrl.Should().Be("https://legacy.test/banner.png");
        result.PosterImageUrl.Should().Be("https://legacy.test/poster.png");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAttachedSchedules()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = Options(connection);
        long bundleId;
        long scheduleId;
        await using (var context = new XBOLDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var now = DateTimeOffset.UtcNow;
            var eventItem = new Event
            {
                Id = 1001,
                Name = "Included Event",
                Status = EventStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now
            };
            var schedule = new EventSchedule
            {
                Event = eventItem,
                StartDateTime = now.AddDays(10),
                EndDateTime = now.AddDays(10).AddHours(2),
                OnSaleDate = now,
                OffSaleDate = now.AddDays(9),
                Status = ScheduleStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now
            };
            var bundle = new Bundle
            {
                Id = 2001,
                Name = "Bundle",
                BundleType = BundleType.Basic,
                BundlePricingType = BundlePricingType.Single,
                Status = EventStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now
            };

            context.Events.Add(eventItem);
            context.EventSchedules.Add(schedule);
            context.Bundles.Add(bundle);
            await context.SaveChangesAsync();

            context.BundleEventSchedules.Add(new BundleEventSchedule
            {
                BundleId = bundle.Id,
                EventScheduleId = schedule.Id,
                SortOrder = 0
            });
            await context.SaveChangesAsync();
            bundleId = bundle.Id;
            scheduleId = schedule.Id;
        }

        await using var readContext = new XBOLDbContext(options);
        var service = CreateBundleService(readContext);

        var result = await service.GetByIdAsync(bundleId);

        result.Should().NotBeNull();
        result!.Schedules.Should().ContainSingle().Which.Id.Should().Be(scheduleId);
    }

    [Fact]
    public async Task GetPagedAsync_UsesBlobAssetUrlsForBannerAndPoster()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = Options(connection);
        long bundleId;
        await using (var context = new XBOLDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var bundle = new Bundle
            {
                Name = "Bundle",
                BundleType = BundleType.Basic,
                BundlePricingType = BundlePricingType.Single,
                Status = EventStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            context.Bundles.Add(bundle);
            await context.SaveChangesAsync();
            bundleId = bundle.Id;

            context.Media.AddRange(
                CreateMedia(bundleId, MediaType.Banner, 0, "banner.png", "https://cdn.test/banner.png"),
                CreateMedia(bundleId, MediaType.Logo, 0, "poster.png", "https://cdn.test/poster.png"));
            await context.SaveChangesAsync();
        }

        await using var readContext = new XBOLDbContext(options);
        var seededBundle = await readContext.Bundles.SingleAsync();
        var service = CreateBundleService(readContext, new BundleSnapshotRepository(seededBundle));

        var result = await service.GetPagedAsync(new BundleQueryParams());

        result.Items.Should().ContainSingle();
        result.Items[0].BannerImageUrl.Should().Be("https://cdn.test/banner.png");
        result.Items[0].PosterImageUrl.Should().Be("https://cdn.test/poster.png");
    }

    [Fact]
    public async Task GetPagedAsync_UsesLowestOrderBlobAssetUrlsForBannerAndPoster()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = Options(connection);
        long bundleId;
        await using (var context = new XBOLDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var bundle = new Bundle
            {
                Name = "Bundle",
                BundleType = BundleType.Basic,
                BundlePricingType = BundlePricingType.Single,
                Status = EventStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            context.Bundles.Add(bundle);
            await context.SaveChangesAsync();
            bundleId = bundle.Id;

            context.Media.AddRange(
                CreateMedia(bundleId, MediaType.Banner, 1, "secondary-banner.png", "https://cdn.test/secondary-banner.png"),
                CreateMedia(bundleId, MediaType.Banner, 0, "primary-banner.png", "https://cdn.test/primary-banner.png"),
                CreateMedia(bundleId, MediaType.Logo, 1, "secondary-poster.png", "https://cdn.test/secondary-poster.png"),
                CreateMedia(bundleId, MediaType.Logo, 0, "primary-poster.png", "https://cdn.test/primary-poster.png"));
            await context.SaveChangesAsync();
        }

        await using var readContext = new XBOLDbContext(options);
        var seededBundle = await readContext.Bundles.SingleAsync();
        var service = CreateBundleService(readContext, new BundleSnapshotRepository(seededBundle));

        var result = await service.GetPagedAsync(new BundleQueryParams());

        result.Items.Should().ContainSingle();
        result.Items[0].BannerImageUrl.Should().Be("https://cdn.test/primary-banner.png");
        result.Items[0].PosterImageUrl.Should().Be("https://cdn.test/primary-poster.png");
    }

    [Fact]
    public async Task GetPagedAsync_KeepsExistingImageUrlsWhenBlobUrlsAreMissing()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = Options(connection);
        long bundleId;
        await using (var context = new XBOLDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var bundle = new Bundle
            {
                Name = "Bundle",
                BundleType = BundleType.Basic,
                BundlePricingType = BundlePricingType.Single,
                Status = EventStatus.Draft,
                BannerImageUrl = "https://legacy.test/banner.png",
                PosterImageUrl = "https://legacy.test/poster.png",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            context.Bundles.Add(bundle);
            await context.SaveChangesAsync();
            bundleId = bundle.Id;

            context.Media.AddRange(
                CreateMedia(bundleId, MediaType.Banner, 0, "banner.png", null),
                CreateMedia(bundleId, MediaType.Logo, 0, "poster.png", null));
            await context.SaveChangesAsync();
        }

        await using var readContext = new XBOLDbContext(options);
        var seededBundle = await readContext.Bundles.SingleAsync();
        var service = CreateBundleService(readContext, new BundleSnapshotRepository(seededBundle));

        var result = await service.GetPagedAsync(new BundleQueryParams());

        result.Items.Should().ContainSingle();
        result.Items[0].BannerImageUrl.Should().Be("https://legacy.test/banner.png");
        result.Items[0].PosterImageUrl.Should().Be("https://legacy.test/poster.png");
    }

    private static BundleService CreateBundleService(
        XBOLDbContext context,
        IBundleRepository? bundleRepository = null)
    {
        bundleRepository ??= new BundleRepository(context);
        var mediaRepository = new MediaRepository(context);
        var mediaService = new MediaService(mediaRepository);
        return new BundleService(
            bundleRepository,
            Substitute.For<IBaseSectionRepository>(),
            Substitute.For<IBundleEventScheduleRepository>(),
            Substitute.For<IEventScheduleRepository>(),
            mediaRepository,
            mediaService,
            Substitute.For<IBundleLifecycleService>());
    }

    private static Media CreateMedia(
        long referenceId,
        MediaType mediaType,
        int order,
        string fileName,
        string? url)
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
                Status = BlobAssetStatus.Available,
                CreatedAt = now,
                UpdatedAt = now
            }
        };
    }

    private static DbContextOptions<XBOLDbContext> Options(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    private sealed class BundleSnapshotRepository(params Bundle[] bundles) : IBundleRepository
    {
        public Task<Bundle?> GetByIdAsync(long id)
        {
            return Task.FromResult(bundles.FirstOrDefault(bundle => bundle.Id == id));
        }

        public Task<Bundle?> GetByIdWithVenueMapAndSchedulesAsync(long id)
        {
            return GetByIdAsync(id);
        }

        public IQueryable<Bundle> Get(
            Expression<Func<Bundle, bool>>? filter = null,
            Func<IQueryable<Bundle>, IOrderedQueryable<Bundle>>? orderBy = null,
            int? pageSize = null,
            int? currentPage = null,
            params string[] includedProperties)
        {
            IQueryable<Bundle> query = bundles.AsQueryable();
            if (filter is not null)
            {
                query = query.Where(filter);
            }

            return orderBy is null ? query : orderBy(query);
        }

        public Task InsertAsync(Bundle entity) => throw new NotSupportedException();
        public Task CommitAsync() => throw new NotSupportedException();
        public Task UpdateAsync(Bundle entity) => throw new NotSupportedException();
        public Task HardDeleteAsync(Bundle entity) => throw new NotSupportedException();
        public Task<List<EventCategory>> GetCategoriesByIdsAsync(IReadOnlyCollection<long> categoryIds) => throw new NotSupportedException();
    }
}
