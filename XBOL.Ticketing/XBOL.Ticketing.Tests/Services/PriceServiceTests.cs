using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.Tests.Services;

public sealed class PriceServiceTests
{
    [Fact]
    public async Task GetSeatsIoPricesAsync_exposes_base_price_fields_for_multi_ticket_type_category()
    {
        await using var database = await TestDatabase.CreateAsync();
        var fixture = await SeedMultiTicketTypeCategoryAsync(database.Context);
        var service = new PriceService(database.Context);

        var result = await service.GetSeatsIoPricesAsync(SaleType.Bundle, fixture.ReferenceId);

        result.Should().ContainSingle();
        var price = result!.Single();
        price.Category.Should().Be(fixture.ExternalZoneKey);
        price.Price.Should().BeNull();
        price.PriceListItemId.Should().BeNull();
        price.BasePrice.Should().Be(1.18m);
        price.BasePriceListItemId.Should().Be(fixture.BasePriceListItemId);
        price.TicketTypes.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSeatsIoPricesAsync_prefers_base_price_before_general_ticket_type_label()
    {
        await using var database = await TestDatabase.CreateAsync();
        var fixture = await SeedMultiTicketTypeCategoryAsync(database.Context);
        var service = new PriceService(database.Context);

        var result = await service.GetSeatsIoPricesAsync(SaleType.Bundle, fixture.ReferenceId);

        var price = result!.Single();
        price.BasePrice.Should().Be(1.18m);
        price.BasePriceListItemId.Should().Be(fixture.BasePriceListItemId);
    }

    [Fact]
    public async Task GetSeatsIoPricesAsync_exposes_base_price_fields_for_multi_ticket_type_object_override()
    {
        await using var database = await TestDatabase.CreateAsync();
        var fixture = await SeedMultiTicketTypeObjectAsync(database.Context);
        var service = new PriceService(database.Context);

        var result = await service.GetSeatsIoPricesAsync(SaleType.Bundle, fixture.ReferenceId);

        result.Should().ContainSingle();
        var price = result!.Single();
        price.Objects.Should().Equal("Club-A-1");
        price.Price.Should().BeNull();
        price.PriceListItemId.Should().BeNull();
        price.BasePrice.Should().Be(1.18m);
        price.BasePriceListItemId.Should().Be(fixture.BasePriceListItemId);
        price.TicketTypes.Should().HaveCount(2);
    }

    private static async Task<PriceFixture> SeedMultiTicketTypeCategoryAsync(XBOLDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var venue = new Venue
        {
            Name = "Estadio",
            AddressLine = "123 Main",
            City = "Tijuana",
            State = "BC",
            Country = "MX",
            Category = VenueCategory.Stadium,
            ShortDescription = "Venue",
            LongDescription = "Venue",
            LogoImageUrl = "",
            BannerImageUrl = "",
            LandingUrl = "",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        var venueMap = new VenueMap
        {
            Venue = venue,
            Name = "Main",
            ExternalMapKey = "main",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        var zone = new BaseZone
        {
            VenueMap = venueMap,
            Name = "Club",
            ExternalZoneKey = 10,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        var priceReference = new PriceReference
        {
            ReferenceType = SaleType.Bundle,
            ReferenceId = 22,
            IsActive = true
        };

        var priceSegment = new PriceSegment
        {
            PriceReference = priceReference,
            BaseZone = zone,
            VenueMap = venueMap,
            PriceItemType = PriceItemType.Zone,
            IsActive = true
        };

        var priceList = new PriceList
        {
            PriceReference = priceReference,
            Status = VersionStatus.Active,
            VersionNumber = 1
        };

        var generalType = new PriceType
        {
            PriceSegment = priceSegment,
            Name = "General",
            IsBasePrice = false,
            Primary = false,
            IsActive = true
        };

        var baseType = new PriceType
        {
            PriceSegment = priceSegment,
            Name = "Xolos Precio Nuevo",
            IsBasePrice = true,
            Primary = true,
            IsActive = true
        };

        var generalPrice = new Price
        {
            PriceSegment = priceSegment,
            PriceType = generalType,
            PriceValue = 2.36m,
            IsActive = true
        };

        var basePrice = new Price
        {
            PriceSegment = priceSegment,
            PriceType = baseType,
            PriceValue = 1.18m,
            IsActive = true
        };

        var generalItem = new PriceListItem
        {
            PriceList = priceList,
            BaseZone = zone,
            Price = generalPrice,
            PriceType = generalType,
            BasePrice = 2.36m,
            FinalPrice = 2.36m
        };

        var baseItem = new PriceListItem
        {
            PriceList = priceList,
            BaseZone = zone,
            Price = basePrice,
            PriceType = baseType,
            BasePrice = 1.18m,
            FinalPrice = 1.18m
        };

        context.AddRange(venue, venueMap, zone, priceReference, priceSegment, priceList, generalType, baseType, generalPrice, basePrice, generalItem, baseItem);
        await context.SaveChangesAsync();

        return new PriceFixture(priceReference.ReferenceId, zone.ExternalZoneKey!.Value, baseItem.Id);
    }

    private static async Task<PriceFixture> SeedMultiTicketTypeObjectAsync(XBOLDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var venue = new Venue
        {
            Name = "Estadio",
            AddressLine = "123 Main",
            City = "Tijuana",
            State = "BC",
            Country = "MX",
            Category = VenueCategory.Stadium,
            ShortDescription = "Venue",
            LongDescription = "Venue",
            LogoImageUrl = "",
            BannerImageUrl = "",
            LandingUrl = "",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        var venueMap = new VenueMap
        {
            Venue = venue,
            Name = "Main",
            ExternalMapKey = "main",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        var zone = new BaseZone
        {
            VenueMap = venueMap,
            Name = "Club Zone",
            ExternalZoneKey = 10,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        var section = new BaseSection
        {
            BaseZone = zone,
            Name = "Club",
            SectionType = SectionType.General,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        var row = new BaseRow
        {
            BaseSection = section,
            RowLabel = "A",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        var seat = new BaseSeat
        {
            BaseRow = row,
            SeatNumber = "1",
            SeatType = SeatType.Standard,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        var priceReference = new PriceReference
        {
            ReferenceType = SaleType.Bundle,
            ReferenceId = 23,
            IsActive = true
        };

        var priceSegment = new PriceSegment
        {
            PriceReference = priceReference,
            BaseZone = zone,
            BaseSection = section,
            BaseRow = row,
            BaseSeat = seat,
            VenueMap = venueMap,
            PriceItemType = PriceItemType.Seat,
            IsActive = true
        };

        var priceList = new PriceList
        {
            PriceReference = priceReference,
            Status = VersionStatus.Active,
            VersionNumber = 1
        };

        var generalType = new PriceType
        {
            PriceSegment = priceSegment,
            Name = "General",
            IsBasePrice = false,
            Primary = false,
            IsActive = true
        };

        var baseType = new PriceType
        {
            PriceSegment = priceSegment,
            Name = "Xolos Precio Nuevo",
            IsBasePrice = true,
            Primary = true,
            IsActive = true
        };

        var generalPrice = new Price
        {
            PriceSegment = priceSegment,
            PriceType = generalType,
            PriceValue = 2.36m,
            IsActive = true
        };

        var basePrice = new Price
        {
            PriceSegment = priceSegment,
            PriceType = baseType,
            PriceValue = 1.18m,
            IsActive = true
        };

        var generalItem = new PriceListItem
        {
            PriceList = priceList,
            BaseSeat = seat,
            Price = generalPrice,
            PriceType = generalType,
            BasePrice = 2.36m,
            FinalPrice = 2.36m
        };

        var baseItem = new PriceListItem
        {
            PriceList = priceList,
            BaseSeat = seat,
            Price = basePrice,
            PriceType = baseType,
            BasePrice = 1.18m,
            FinalPrice = 1.18m
        };

        context.AddRange(venue, venueMap, zone, section, row, seat, priceReference, priceSegment, priceList, generalType, baseType, generalPrice, basePrice, generalItem, baseItem);
        await context.SaveChangesAsync();

        return new PriceFixture(priceReference.ReferenceId, zone.ExternalZoneKey!.Value, baseItem.Id);
    }

    private sealed record PriceFixture(long ReferenceId, long ExternalZoneKey, long BasePriceListItemId);

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestDatabase(SqliteConnection connection, XBOLDbContext context)
        {
            this.connection = connection;
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

            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
