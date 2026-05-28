using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Services.Event;

namespace XBOL.Ticketing.Tests.Services;

public class EventCatalogServiceTests
{
    [Fact]
    public async Task GetItemsAsync_ReturnsStandaloneEventsAndBundlesForUpcomingCatalog()
    {
        await using var database = await TestDatabase.CreateAsync();
        var sut = new EventCatalogService(database.Context);

        var result = await sut.GetItemsAsync(new EventCatalogQueryParams
        {
            Upcoming = true,
            Page = 1,
            PageSize = 10,
            SortBy = "startDate",
            Descending = false
        });

        result.TotalCount.Should().Be(8);
        var identities = result.Items.Select(item => (item.Id, item.ItemType)).ToList();
        identities.Should().Contain((1, EventCatalogItemType.Event));
        identities.Should().Contain((70, EventCatalogItemType.Event));
        identities.Should().Contain((30, EventCatalogItemType.Bundle));
        identities.Should().Contain((40, EventCatalogItemType.Bundle));
    }

    [Fact]
    public async Task GetItemsAsync_FiltersBundlesByTypeStatusVenueAndEventScheduleDate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var sut = new EventCatalogService(database.Context);

        var result = await sut.GetItemsAsync(new EventCatalogQueryParams
        {
            ItemType = EventCatalogItemType.Bundle,
            BundleType = BundleType.SeasonPass,
            Status = EventStatus.Published,
            Venue = "Estadio Caliente",
            StartDate = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2026, 11, 30, 23, 59, 59, TimeSpan.Zero),
            Page = 1,
            PageSize = 10
        });

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Id = 30L,
            Name = "Xolopack",
            ItemType = EventCatalogItemType.Bundle,
            BundleType = BundleType.SeasonPass,
            Status = EventStatus.Published,
            ScheduledStartDate = new DateTimeOffset(2026, 11, 15, 19, 0, 0, TimeSpan.Zero),
            VenueName = "Estadio Caliente",
            AvailableSeats = 16523,
            TotalSeats = 27000,
            IsSeason = true
        });
    }

    [Fact]
    public async Task GetItemsAsync_UsesAvailableEventBannerBlobAssetForEventImageUrls()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Context.Media.AddRange(
            CreateMedia(70, SaleType.Event, MediaType.Banner, 0, BlobAssetStatus.PendingUpload, "pending-event.png", "https://cdn.test/pending-event.png"),
            CreateMedia(70, SaleType.Event, MediaType.Banner, 1, BlobAssetStatus.Available, "event-banner.png", "https://cdn.test/event-banner.png"),
            CreateMedia(70, SaleType.Event, MediaType.Banner, 2, BlobAssetStatus.Available, "deleted-event.png", "https://cdn.test/deleted-event.png", DateTimeOffset.UtcNow));
        await database.Context.SaveChangesAsync();

        var sut = new EventCatalogService(database.Context);

        var result = await sut.GetItemsAsync(new EventCatalogQueryParams
        {
            ItemType = EventCatalogItemType.Event,
            SearchTerm = "Standalone Event",
            Upcoming = true,
            Page = 1,
            PageSize = 10
        });

        result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Id = 70L,
            BannerImageUrl = "https://cdn.test/event-banner.png",
            PosterImageUrl = "https://cdn.test/event-banner.png"
        });
    }

    [Fact]
    public async Task GetItemsAsync_UsesAvailableBundleBlobAssetsForBundleImageUrls()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Context.Media.AddRange(
            CreateMedia(30, SaleType.Bundle, MediaType.Banner, 0, BlobAssetStatus.PendingUpload, "pending-bundle-banner.png", "https://cdn.test/pending-bundle-banner.png"),
            CreateMedia(30, SaleType.Bundle, MediaType.Banner, 1, BlobAssetStatus.Available, "bundle-banner.png", "https://cdn.test/bundle-banner.png"),
            CreateMedia(30, SaleType.Bundle, MediaType.Logo, 0, BlobAssetStatus.PendingUpload, "pending-bundle-logo.png", "https://cdn.test/pending-bundle-logo.png"),
            CreateMedia(30, SaleType.Bundle, MediaType.Logo, 1, BlobAssetStatus.Available, "bundle-logo.png", "https://cdn.test/bundle-logo.png"),
            CreateMedia(30, SaleType.Bundle, MediaType.Banner, 2, BlobAssetStatus.Available, "deleted-bundle-banner.png", "https://cdn.test/deleted-bundle-banner.png", DateTimeOffset.UtcNow));
        await database.Context.SaveChangesAsync();

        var sut = new EventCatalogService(database.Context);

        var result = await sut.GetItemsAsync(new EventCatalogQueryParams
        {
            ItemType = EventCatalogItemType.Bundle,
            Status = EventStatus.Published,
            SearchTerm = "Xolopack",
            Upcoming = true,
            Page = 1,
            PageSize = 10
        });

        result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Id = 30L,
            BannerImageUrl = "https://cdn.test/bundle-banner.png",
            PosterImageUrl = "https://cdn.test/bundle-logo.png"
        });
    }

    [Fact]
    public async Task GetBundleScheduleItemsAsync_FiltersByVenueAndDateRangeAndPaginates()
    {
        await using var database = await TestDatabase.CreateAsync();
        var sut = new EventCatalogService(database.Context);

        var result = await sut.GetBundleScheduleItemsAsync(30, new BundleScheduleQueryParams
        {
            Venue = "Estadio Caliente",
            StartDate = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero),
            Page = 1,
            PageSize = 1,
            SortBy = "startDate",
            Descending = false
        });

        result.TotalCount.Should().Be(2);
        result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            EventId = 1L,
            EventScheduleId = 10L,
            Name = "Tigres vs. Toluca",
            ScheduledStartDate = new DateTimeOffset(2026, 11, 15, 19, 0, 0, TimeSpan.Zero),
            VenueName = "Estadio Caliente",
            AvailableSeats = 9821,
            TotalSeats = 27000
        });
    }

    private static Media CreateMedia(
        long referenceId,
        SaleType referenceType,
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
            ReferenceType = referenceType,
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

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private TestDatabase(SqliteConnection connection, XBOLDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public XBOLDbContext Context { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<XBOLDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new XBOLDbContext(options);
            await context.Database.EnsureCreatedAsync();
            Seed(context);
            await context.SaveChangesAsync();

            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private static void Seed(XBOLDbContext context)
        {
            var now = new DateTimeOffset(2026, 5, 26, 12, 0, 0, TimeSpan.Zero);

            context.Venues.AddRange(
                Venue(1, "Estadio Caliente"),
                Venue(2, "Auditorio Nacional"));

            context.VenueMaps.AddRange(
                VenueMap(1, 1),
                VenueMap(2, 2));

            context.BaseZones.AddRange(
                BaseZone(1, 1),
                BaseZone(2, 2));

            context.BaseSections.AddRange(
                BaseSection(10, 1),
                BaseSection(11, 1),
                BaseSection(12, 2),
                BaseSection(13, 1),
                BaseSection(14, 1),
                BaseSection(30, 1),
                BaseSection(40, 1),
                BaseSection(50, 1),
                BaseSection(60, 2));

            context.Events.AddRange(
                Event(1, "Tigres vs. Toluca", 1, EventStatus.Published),
                Event(2, "Toluca vs. Tigres", 1, EventStatus.Published),
                Event(3, "Concert Night", 2, EventStatus.Published),
                Event(4, "Past Match", 1, EventStatus.Published),
                Event(70, "Standalone Event", 1, EventStatus.Published));

            context.EventSchedules.AddRange(
                Schedule(10, 1, new DateTimeOffset(2026, 11, 15, 19, 0, 0, TimeSpan.Zero), 9821, 27000),
                Schedule(11, 2, new DateTimeOffset(2026, 12, 14, 19, 0, 0, TimeSpan.Zero), 1531, 27000),
                Schedule(12, 3, new DateTimeOffset(2026, 11, 20, 20, 0, 0, TimeSpan.Zero), 100, 5000),
                Schedule(13, 4, new DateTimeOffset(2026, 1, 15, 19, 0, 0, TimeSpan.Zero), 10, 27000),
                Schedule(14, 70, new DateTimeOffset(2026, 11, 25, 19, 0, 0, TimeSpan.Zero), 10, 27000));

            context.Bundles.AddRange(
                Bundle(30, "Xolopack", 1, BundleType.SeasonPass, EventStatus.Published, now, 16523, 27000),
                Bundle(40, "Apertura Package", 1, BundleType.Basic, EventStatus.Published, now, 250, 1000),
                Bundle(50, "Draft Xolopack", 1, BundleType.SeasonPass, EventStatus.Draft, now, 1, 100),
                Bundle(60, "Tour Package", 2, BundleType.SeasonPass, EventStatus.Published, now, 1, 100));

            context.BundleEventSchedules.AddRange(
                new BundleEventSchedule { BundleId = 30, EventScheduleId = 10, SortOrder = 1 },
                new BundleEventSchedule { BundleId = 30, EventScheduleId = 11, SortOrder = 2 },
                new BundleEventSchedule { BundleId = 40, EventScheduleId = 10, SortOrder = 1 },
                new BundleEventSchedule { BundleId = 50, EventScheduleId = 10, SortOrder = 1 },
                new BundleEventSchedule { BundleId = 60, EventScheduleId = 12, SortOrder = 1 });
        }

        private static Venue Venue(long id, string name) => new()
        {
            Id = id,
            Name = name,
            AddressLine = "Blvd. Agua Caliente",
            City = "Tijuana",
            State = "BC",
            Country = "MX",
            ShortDescription = name,
            LongDescription = name,
            LogoImageUrl = "",
            BannerImageUrl = "",
            LandingUrl = "",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        private static VenueMap VenueMap(long id, long venueId) => new()
        {
            Id = id,
            VenueId = venueId,
            Name = $"Map {id}",
            ExternalMapKey = $"map-{id}",
            Capacity = 27000,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        private static BaseZone BaseZone(long id, long venueMapId) => new()
        {
            Id = id,
            VenueMapId = venueMapId,
            Name = $"Zone {id}"
        };

        private static BaseSection BaseSection(long id, long baseZoneId) => new()
        {
            Id = id,
            BaseZoneId = baseZoneId,
            Name = $"Section {id}"
        };

        private static Event Event(long id, string name, long venueMapId, EventStatus status) => new()
        {
            Id = id,
            Name = name,
            VenueMapId = venueMapId,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        private static EventSchedule Schedule(
            long id,
            long eventId,
            DateTimeOffset start,
            int available,
            int total) => new()
        {
            Id = id,
            EventId = eventId,
            Status = ScheduleStatus.OnSale,
            StartDateTime = start,
            EndDateTime = start.AddHours(3),
            OnSaleDate = start.AddMonths(-1),
            OffSaleDate = start.AddHours(-1),
            ExternalEventKey = $"event-{id}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Sections =
            [
                new EventSection
                {
                    BaseSectionId = id,
                    DisplayName = "General",
                    AvailableSeats = available,
                    TotalSeats = total
                }
            ]
        };

        private static Bundle Bundle(
            long id,
            string name,
            long venueMapId,
            BundleType bundleType,
            EventStatus status,
            DateTimeOffset now,
            int available,
            int total) => new()
        {
            Id = id,
            Name = name,
            VenueMapId = venueMapId,
            BundleType = bundleType,
            BundlePricingType = BundlePricingType.Composite,
            Status = status,
            StartDate = now,
            EndDate = now.AddMonths(8),
            CreatedAt = now,
            UpdatedAt = now,
            BundleSections =
            [
                new BundleSection
                {
                    BaseSectionId = id,
                    DisplayName = "General",
                    AvailableSeats = available,
                    TotalSeats = total
                }
            ]
        };
    }
}
