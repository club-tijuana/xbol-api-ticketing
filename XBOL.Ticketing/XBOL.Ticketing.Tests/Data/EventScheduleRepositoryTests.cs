using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Repositories.Event;

namespace XBOL.Ticketing.Tests.Data;

public sealed class EventScheduleRepositoryTests
{
    [Fact]
    public async Task GetByIdWithEventAndVenueMapAsync_LoadsSectionsForScheduleTotals()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;
        long scheduleId;

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
                ExternalMapKey = "map",
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
                Name = "Section",
                SectionType = SectionType.General
            };
            var eventItem = new Event
            {
                Id = 5,
                VenueMap = venueMap,
                Name = "Event",
                Status = EventStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now
            };
            var schedule = new EventSchedule
            {
                Id = 6,
                Event = eventItem,
                StartDateTime = now.AddDays(3),
                EndDateTime = now.AddDays(3).AddHours(2),
                OnSaleDate = now,
                OffSaleDate = now.AddDays(2),
                Status = ScheduleStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now,
                Sections =
                [
                    new EventSection
                    {
                        BaseSection = baseSection,
                        DisplayName = "General",
                        TotalSeats = 100,
                        AvailableSeats = 75
                    }
                ]
            };

            context.EventSchedules.Add(schedule);
            await context.SaveChangesAsync();
            scheduleId = schedule.Id;
        }

        await using var readContext = new XBOLDbContext(options);
        var repository = new EventScheduleRepository(readContext);

        var result = await repository.GetByIdWithEventAndVenueMapAsync(scheduleId);

        result.Should().NotBeNull();
        result!.Event.VenueMap.Should().NotBeNull();
        result.Sections.Should().ContainSingle();
        result.Sections.Single().TotalSeats.Should().Be(100);
        result.Sections.Single().AvailableSeats.Should().Be(75);
    }
}
