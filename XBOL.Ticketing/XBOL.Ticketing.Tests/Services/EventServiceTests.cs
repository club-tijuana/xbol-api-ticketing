using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Repositories.Bundle;
using XBOL.Ticketing.Data.Repositories.Event;
using XBOL.Ticketing.Services.Event;

namespace XBOL.Ticketing.Tests.Services;

public class EventServiceTests
{
    [Fact]
    public async Task CancelEventAsync_CascadesToStandaloneAndBasicLinkedSchedules()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = Options(connection);
        await SeedAsync(options);

        await using var context = new XBOLDbContext(options);
        var lifecycleService = Substitute.For<IEventScheduleLifecycleService>();
        lifecycleService.CancelAsync(Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var userId = Guid.NewGuid();
        var sut = Service(context, lifecycleService);

        var result = await sut.CancelEventAsync(1, userId);

        result.Should().BeTrue();
        await lifecycleService.Received(1).CancelAsync(10, userId, Arg.Any<CancellationToken>());
        await lifecycleService.Received(1).CancelAsync(11, userId, Arg.Any<CancellationToken>());
        await lifecycleService.DidNotReceive().CancelAsync(12, Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        context.ChangeTracker.Clear();
        var persisted = await context.Events.AsNoTracking().SingleAsync(e => e.Id == 1);
        persisted.Status.Should().Be(EventStatus.Cancelled);
    }

    [Fact]
    public async Task CancelEventAsync_NotFound_ReturnsFalseAndDoesNotCascade()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = Options(connection);
        await using (var setupContext = new XBOLDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
        }

        await using var context = new XBOLDbContext(options);
        var lifecycleService = Substitute.For<IEventScheduleLifecycleService>();
        var sut = Service(context, lifecycleService);

        var result = await sut.CancelEventAsync(999, Guid.NewGuid());

        result.Should().BeFalse();
        await lifecycleService.DidNotReceive()
            .CancelAsync(Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static EventService Service(
        XBOLDbContext context,
        IEventScheduleLifecycleService lifecycleService)
    {
        return new EventService(
            new EventRepository(context),
            null!,
            null!,
            null!,
            new EventScheduleRepository(context),
            null!,
            lifecycleService,
            new BundleEventScheduleRepository(context));
    }

    private static DbContextOptions<XBOLDbContext> Options(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    private static async Task SeedAsync(DbContextOptions<XBOLDbContext> options)
    {
        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();

        context.Events.Add(new Event
        {
            Id = 1,
            Name = "Opening Match",
            Status = EventStatus.Published
        });
        context.EventSchedules.AddRange(
            Schedule(10),
            Schedule(11),
            Schedule(12));
        context.Bundles.AddRange(
            Bundle(20, BundleType.Basic),
            Bundle(30, BundleType.SeasonPass));
        context.BundleEventSchedules.AddRange(
            new BundleEventSchedule { BundleId = 20, EventScheduleId = 11 },
            new BundleEventSchedule { BundleId = 30, EventScheduleId = 12 });

        await context.SaveChangesAsync();
    }

    private static EventSchedule Schedule(long id)
    {
        return new EventSchedule
        {
            Id = id,
            EventId = 1,
            Status = ScheduleStatus.OnSale,
            ExternalEventKey = $"schedule-{id}",
            StartDateTime = new DateTimeOffset(2026, 6, 1, 19, 0, 0, TimeSpan.Zero),
            EndDateTime = new DateTimeOffset(2026, 6, 1, 22, 0, 0, TimeSpan.Zero),
            OnSaleDate = new DateTimeOffset(2026, 5, 25, 10, 0, 0, TimeSpan.Zero),
            OffSaleDate = new DateTimeOffset(2026, 6, 1, 18, 0, 0, TimeSpan.Zero)
        };
    }

    private static Bundle Bundle(long id, BundleType bundleType)
    {
        return new Bundle
        {
            Id = id,
            Name = $"{bundleType} Bundle",
            Status = EventStatus.Published,
            BundleType = bundleType,
            BundlePricingType = BundlePricingType.Composite,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
