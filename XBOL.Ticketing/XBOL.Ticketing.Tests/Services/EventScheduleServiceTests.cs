using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Repositories.Event;
using XBOL.Ticketing.Services.Event;

namespace XBOL.Ticketing.Tests.Services;

public class EventScheduleServiceTests
{
    [Fact]
    public async Task CreateEventScheduleAsync_PersistsAuditFields()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var seedContext = new XBOLDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Events.Add(Event());
            await seedContext.SaveChangesAsync();
        }

        await using var context = new XBOLDbContext(options);
        var sut = new EventScheduleService(
            new EventScheduleRepository(context),
            context,
            Substitute.For<IEventScheduleLifecycleService>());
        var userId = Guid.NewGuid();

        var response = await sut.CreateEventScheduleAsync(CreateRequest(), userId);

        context.ChangeTracker.Clear();
        var persisted = await context.EventSchedules
            .AsNoTracking()
            .SingleAsync(s => s.Id == response.Id);

        persisted.Status.Should().Be(ScheduleStatus.Draft);
        persisted.CreatedAt.Should().NotBe(default);
        persisted.UpdatedAt.Should().Be(persisted.CreatedAt);
        persisted.CreatedBy.Should().Be(userId);
        persisted.UpdatedBy.Should().Be(userId);
    }

    [Fact]
    public async Task UpdateScheduleAsync_MetadataSyncFailure_PreservesLocalScheduleUpdate()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var seedContext = new XBOLDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Events.Add(Event());
            seedContext.EventSchedules.Add(Schedule());
            await seedContext.SaveChangesAsync();
        }

        await using var context = new XBOLDbContext(options);
        var lifecycleService = Substitute.For<IEventScheduleLifecycleService>();
        lifecycleService.SyncMetadataAsync(10, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("metadata sync failed")));

        var sut = new EventScheduleService(
            new EventScheduleRepository(context),
            context,
            lifecycleService);

        var request = CreateRequest();

        var act = () => sut.UpdateScheduleAsync(10, request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("metadata sync failed");

        context.ChangeTracker.Clear();
        var persisted = await context.EventSchedules
            .AsNoTracking()
            .SingleAsync(s => s.Id == 10);

        persisted.ExternalEventKey.Should().Be("schedule-10");
        persisted.StartDateTime.Should().Be(request.StartDateTime.ToUniversalTime());
        persisted.EndDateTime.Should().Be(request.EndDateTime.ToUniversalTime());
        persisted.OnSaleDate.Should().Be(request.OnSaleDate.ToUniversalTime());
        persisted.OffSaleDate.Should().Be(request.OffSaleDate.ToUniversalTime());
        persisted.PublishedDate.Should().Be(request.PublishedDate.GetValueOrDefault().ToUniversalTime());
        persisted.GateOpenDate.Should().Be(request.GateOpenDate.GetValueOrDefault().ToUniversalTime());
        persisted.GameCategory.Should().Be(GameCategory.Playoff);
        persisted.HoldExpirationInMinutes.Should().Be(12);
    }

    private static Event Event()
    {
        return new Event
        {
            Id = 1,
            Name = "Opening Match",
            Status = EventStatus.Published
        };
    }

    private static EventSchedule Schedule()
    {
        return new EventSchedule
        {
            Id = 10,
            EventId = 1,
            ExternalEventKey = "schedule-10",
            Status = ScheduleStatus.OnSale,
            StartDateTime = new DateTimeOffset(2026, 6, 1, 19, 0, 0, TimeSpan.Zero),
            EndDateTime = new DateTimeOffset(2026, 6, 1, 22, 0, 0, TimeSpan.Zero),
            OnSaleDate = new DateTimeOffset(2026, 5, 25, 10, 0, 0, TimeSpan.Zero),
            OffSaleDate = new DateTimeOffset(2026, 6, 1, 18, 0, 0, TimeSpan.Zero),
            GameCategory = GameCategory.Regular,
            HoldExpirationInMinutes = 5
        };
    }

    private static EventScheduleRequest CreateRequest()
    {
        return new EventScheduleRequest
        {
            EventId = 1,
            PreSaleStartDate = new DateTimeOffset(2026, 5, 27, 9, 0, 0, TimeSpan.FromHours(-7)),
            PreSaleEndDate = new DateTimeOffset(2026, 5, 27, 12, 0, 0, TimeSpan.FromHours(-7)),
            OnSaleDate = new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.FromHours(-7)),
            OffSaleDate = new DateTimeOffset(2026, 6, 10, 18, 0, 0, TimeSpan.FromHours(-7)),
            PublishedDate = new DateTimeOffset(2026, 5, 28, 10, 5, 0, TimeSpan.FromHours(-7)),
            GateOpenDate = new DateTimeOffset(2026, 6, 10, 17, 0, 0, TimeSpan.FromHours(-7)),
            StartDateTime = new DateTimeOffset(2026, 6, 10, 19, 0, 0, TimeSpan.FromHours(-7)),
            EndDateTime = new DateTimeOffset(2026, 6, 10, 22, 0, 0, TimeSpan.FromHours(-7)),
            GameCategory = GameCategory.Playoff,
            HoldExpirationInMinutes = 12
        };
    }
}
