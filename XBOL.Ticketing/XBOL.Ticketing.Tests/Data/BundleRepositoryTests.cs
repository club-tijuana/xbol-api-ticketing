using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Repositories.Bundle;

namespace XBOL.Ticketing.Tests.Data;

public sealed class BundleRepositoryTests
{
    [Fact]
    public async Task GetByIdWithVenueMapAndSchedulesAsync_DoesNotMaterializeVenue()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var commandCounter = new SelectCommandCounter();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(commandCounter)
            .Options;
        long bundleId;

        await using (var context = new XBOLDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var now = DateTimeOffset.UtcNow;
            var venue = new Venue
            {
                Id = 1,
                Name = "Venue",
                AddressLine = "Street",
                City = "Tijuana",
                State = "BC",
                Country = "Mexico",
                ShortDescription = "Venue",
                LongDescription = "Venue",
                LogoImageUrl = "",
                BannerImageUrl = "",
                LandingUrl = "",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            var venueMap = new VenueMap
            {
                Id = 2,
                Venue = venue,
                Name = "Map",
                ExternalMapKey = "chart-key",
                CreatedAt = now,
                UpdatedAt = now
            };
            var baseZone = new BaseZone
            {
                Id = 3,
                VenueMap = venueMap,
                Name = "Zone"
            };
            var baseSection = new BaseSection
            {
                Id = 4,
                BaseZone = baseZone,
                Name = "B",
                SectionType = SectionType.General
            };
            var eventItem = new Event
            {
                Id = 5,
                VenueMap = venueMap,
                Name = "Event",
                Status = EventStatus.Published,
                CreatedAt = now,
                UpdatedAt = now
            };
            var category = new EventCategory
            {
                Id = 8,
                Name = "sports",
                DisplayName = "Sports",
                IsActive = true
            };
            var schedule = new EventSchedule
            {
                Id = 6,
                Event = eventItem,
                StartDateTime = now.AddDays(3),
                EndDateTime = now.AddDays(3).AddHours(2),
                OnSaleDate = now,
                OffSaleDate = now.AddDays(2),
                Status = ScheduleStatus.OnSale,
                CreatedAt = now,
                UpdatedAt = now,
                Sections =
                [
                    new EventSection
                    {
                        BaseSection = baseSection,
                        DisplayName = "B",
                        TotalSeats = 1,
                        AvailableSeats = 1
                    }
                ]
            };
            var bundle = new Bundle
            {
                Id = 7,
                VenueMap = venueMap,
                Name = "Basic Bundle",
                Status = EventStatus.Draft,
                BundleType = BundleType.Basic,
                BundlePricingType = BundlePricingType.Composite,
                CreatedAt = now,
                UpdatedAt = now,
                Categories = [category],
                BundleEventSchedules =
                [
                    new BundleEventSchedule
                    {
                        EventSchedule = schedule,
                        SortOrder = 0
                    }
                ]
            };

            context.Bundles.Add(bundle);
            await context.SaveChangesAsync();
            bundleId = bundle.Id;
        }

        await using var readContext = new XBOLDbContext(options);
        var repository = new BundleRepository(readContext);

        var result = await repository.GetByIdWithVenueMapAndSchedulesAsync(bundleId);

        result.Should().NotBeNull();
        result!.VenueMap.Should().NotBeNull();
        result.VenueMap!.ExternalMapKey.Should().Be("chart-key");
        result.VenueMap.Venue.Should().BeNull();
        result.Categories.Should().ContainSingle().Which.DisplayName.Should().Be("Sports");
        result.BundleEventSchedules.Should().ContainSingle();
        result.BundleEventSchedules.Single().EventSchedule.Sections.Should().ContainSingle();
        commandCounter.SelectCommandCount.Should().BeGreaterThan(1);
    }

    private sealed class SelectCommandCounter : DbCommandInterceptor
    {
        public int SelectCommandCount { get; private set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            CountSelect(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CountSelect(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void CountSelect(DbCommand command)
        {
            if (command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                SelectCommandCount++;
            }
        }
    }
}
