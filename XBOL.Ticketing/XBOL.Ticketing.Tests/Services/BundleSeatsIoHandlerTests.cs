using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Data.Repositories.Bundle;
using XBOL.Ticketing.Services;
using XBOL.Ticketing.Services.Bundle;
using XBOL.Ticketing.Services.Messages;

namespace XBOL.Ticketing.Tests.Services;

public class BundleSeatsIoHandlerTests
{
    private readonly IBundleRepository _bundleRepository = Substitute.For<IBundleRepository>();
    private readonly ISeatsIoSeasonLifecycleClient _seatsIo = Substitute.For<ISeatsIoSeasonLifecycleClient>();
    private readonly IBundlePassTicketMaterializationService _ticketMaterializer = Substitute.For<IBundlePassTicketMaterializationService>();

    [Fact]
    public async Task CreateSeatsIoSeasonHandler_CreatesSeasonAndPersistsBundleAndScheduleKeys()
    {
        var bundle = SeasonPassBundle(20,
        [
            ScheduleLink(20, 10),
            ScheduleLink(20, 11)
        ]);
        _bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(20).Returns(bundle);

        var sut = Handler();

        await sut.Handle(new CreateSeatsIoSeasonCommand(20, Guid.Empty));

        bundle.ExternalKey.Should().MatchRegex("^season-20-[0-9a-f]{32}$");
        var seasonKey = bundle.ExternalKey!;
        await _seatsIo.Received(1).CreateSeatsIoSeasonAsync(
            "chart-main",
            seasonKey,
            Arg.Is<string[]>(eventKeys =>
                eventKeys.SequenceEqual(new[]
                {
                    $"{seasonKey}-schedule-10",
                    $"{seasonKey}-schedule-11"
                })));
        bundle.Status.Should().Be(EventStatus.Published);
        bundle.PublishedDate.Should().NotBeNull();
        bundle.BundleEventSchedules.Select(link => link.EventSchedule.ExternalEventKey)
            .Should().Equal($"{seasonKey}-schedule-10", $"{seasonKey}-schedule-11");
        bundle.BundleEventSchedules.Select(link => link.EventSchedule.Status)
            .Should().Equal(ScheduleStatus.OnSale, ScheduleStatus.OnSale);
        bundle.BundleEventSchedules.Select(link => link.EventSchedule.PublishedDate)
            .Should().OnlyContain(publishedDate => publishedDate.HasValue);
        await _ticketMaterializer.Received(1).MaterializeIssuedTicketsAsync(
            20,
            Arg.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new[] { 10L, 11L })),
            Guid.Empty,
            Arg.Any<CancellationToken>());
        await _bundleRepository.Received(1).UpdateAsync(bundle);
    }

    [Fact]
    public async Task CreateSeatsIoSeasonHandler_NewBundleGeneratesUniqueSeasonKey()
    {
        var bundle = SeasonPassBundle(20, [ScheduleLink(20, 10)]);
        _bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(20).Returns(bundle);

        var sut = Handler();

        await sut.Handle(new CreateSeatsIoSeasonCommand(20, Guid.Empty));

        bundle.ExternalKey.Should().MatchRegex("^season-20-[0-9a-f]{32}$");
        bundle.BundleEventSchedules[0].EventSchedule.ExternalEventKey.Should()
            .Be($"{bundle.ExternalKey}-schedule-10");
        await _seatsIo.Received(1).CreateSeatsIoSeasonAsync(
            "chart-main",
            bundle.ExternalKey,
            Arg.Is<string[]>(eventKeys => eventKeys.SequenceEqual(new[] { $"{bundle.ExternalKey}-schedule-10" })));
    }

    [Fact]
    public async Task CreateSeatsIoSeasonHandler_WithoutLinkedSchedules_CreatesSeasonWithEmptyEvents()
    {
        var bundle = SeasonPassBundle(20, []);
        _bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(20).Returns(bundle);

        var sut = Handler();

        await sut.Handle(new CreateSeatsIoSeasonCommand(20, Guid.Empty));

        bundle.ExternalKey.Should().MatchRegex("^season-20-[0-9a-f]{32}$");
        await _seatsIo.Received(1).CreateSeatsIoSeasonAsync(
            "chart-main",
            bundle.ExternalKey,
            Arg.Is<string[]>(keys => keys.Length == 0));
        bundle.Status.Should().Be(EventStatus.Published);
        await _bundleRepository.Received(1).UpdateAsync(bundle);
    }

    [Fact]
    public async Task CreateSeatsIoSeasonHandler_DbFailureDeletesCreatedRemoteSeason()
    {
        var bundle = SeasonPassBundle(20, [ScheduleLink(20, 10)]);
        _bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(20).Returns(bundle);
        _bundleRepository.UpdateAsync(bundle)
            .Returns(Task.FromException(new InvalidOperationException("database failure")));

        var sut = Handler();

        var act = () => sut.Handle(new CreateSeatsIoSeasonCommand(20, Guid.Empty));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("database failure");
        bundle.ExternalKey.Should().MatchRegex("^season-20-[0-9a-f]{32}$");
        await _seatsIo.Received(1).CreateSeatsIoSeasonAsync(
            "chart-main",
            bundle.ExternalKey,
            Arg.Any<string[]>());
        await _seatsIo.Received(1).DeleteSeatsIoSeasonAsync(bundle.ExternalKey);
    }

    [Fact]
    public async Task CreateSeatsIoSeasonHandler_ExistingExternalKey_CreatesRemoteSeasonWithExistingKey()
    {
        var bundle = SeasonPassBundle(20, [ScheduleLink(20, 10)]);
        bundle.ExternalKey = "xcl2027-abridged-renewal";
        _bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(20).Returns(bundle);

        var sut = Handler();

        await sut.Handle(new CreateSeatsIoSeasonCommand(20, Guid.Empty));

        await _seatsIo.Received(1).CreateSeatsIoSeasonAsync(
            "chart-main",
            "xcl2027-abridged-renewal",
            Arg.Is<string[]>(eventKeys => eventKeys.SequenceEqual(new[] { "xcl2027-abridged-renewal-schedule-10" })));
        bundle.ExternalKey.Should().Be("xcl2027-abridged-renewal");
        bundle.BundleEventSchedules[0].EventSchedule.ExternalEventKey.Should()
            .Be("xcl2027-abridged-renewal-schedule-10");
        bundle.Status.Should().Be(EventStatus.Published);
        await _bundleRepository.Received(1).UpdateAsync(bundle);
    }

    [Fact]
    public async Task AddEventsToSeasonHandler_CreatesEventsInSeasonAndPersistsScheduleKeys()
    {
        var bundle = SeasonPassBundle(20,
        [
            ScheduleLink(20, 10),
            ScheduleLink(20, 11)
        ]);
        bundle.ExternalKey = "season-20";
        _bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(20).Returns(bundle);

        var sut = AddEventsHandler();

        await sut.Handle(new AddEventsToSeasonCommand(20, [10, 11]));

        await _seatsIo.Received(1).CreateSeatsIoEventsInSeasonAsync(
            "season-20",
            Arg.Is<string[]>(eventKeys =>
                eventKeys.SequenceEqual(new[]
                {
                    "season-20-schedule-10",
                    "season-20-schedule-11"
                })));
        bundle.BundleEventSchedules.Select(link => link.EventSchedule.ExternalEventKey)
            .Should().Equal("season-20-schedule-10", "season-20-schedule-11");
        bundle.BundleEventSchedules.Select(link => link.EventSchedule.Status)
            .Should().Equal(ScheduleStatus.OnSale, ScheduleStatus.OnSale);
        bundle.BundleEventSchedules.Select(link => link.EventSchedule.PublishedDate)
            .Should().OnlyContain(publishedDate => publishedDate.HasValue);
        await _ticketMaterializer.Received(1).MaterializeIssuedTicketsAsync(
            20,
            Arg.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new[] { 10L, 11L })),
            Guid.Empty,
            Arg.Any<CancellationToken>());
        await _bundleRepository.Received(1).UpdateAsync(bundle);
    }

    [Fact]
    public async Task AddEventsToSeasonHandler_DbFailureDeletesCreatedRemoteEvents()
    {
        var bundle = SeasonPassBundle(20, [ScheduleLink(20, 10)]);
        bundle.ExternalKey = "season-20";
        _bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(20).Returns(bundle);
        _bundleRepository.UpdateAsync(bundle)
            .Returns(Task.FromException(new InvalidOperationException("database failure")));

        var sut = AddEventsHandler();

        var act = () => sut.Handle(new AddEventsToSeasonCommand(20, [10]));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("database failure");
        await _seatsIo.Received(1).CreateSeatsIoEventsInSeasonAsync(
            "season-20",
            Arg.Is<string[]>(eventKeys => eventKeys.SequenceEqual(new[] { "season-20-schedule-10" })));
        await _seatsIo.Received(1).DeleteSeatsIoEventAsync("season-20-schedule-10");
    }

    [Fact]
    public async Task AddEventsToSeasonHandler_DbFailureWithRealRepositoryDeletesCreatedRemoteEvents()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var seedContext = new XBOLDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Venues.Add(new Venue
            {
                Id = 3,
                Name = "Main Venue",
                AddressLine = "123 Main",
                City = "Tijuana",
                State = "BC",
                Country = "MX",
                ShortDescription = "Main",
                LongDescription = "Main venue",
                LogoImageUrl = string.Empty,
                BannerImageUrl = string.Empty,
                LandingUrl = string.Empty,
                IsActive = true
            });
            seedContext.VenueMaps.Add(new VenueMap
            {
                Id = 2,
                VenueId = 3,
                Name = "Main chart",
                ExternalMapKey = "chart-main"
            });
            seedContext.Events.Add(new Event
            {
                Id = 1,
                Name = "Opening Match",
                Status = EventStatus.Published
            });
            seedContext.Bundles.Add(new Bundle
            {
                Id = 20,
                VenueMapId = 2,
                Name = "Season Pass",
                BundleType = BundleType.SeasonPass,
                BundlePricingType = BundlePricingType.Composite,
                Status = EventStatus.Published,
                ExternalKey = "season-20"
            });
            seedContext.EventSchedules.Add(new EventSchedule
            {
                Id = 10,
                EventId = 1,
                Status = ScheduleStatus.Draft,
                StartDateTime = new DateTimeOffset(2026, 6, 1, 19, 0, 0, TimeSpan.Zero),
                EndDateTime = new DateTimeOffset(2026, 6, 1, 22, 0, 0, TimeSpan.Zero),
                OnSaleDate = new DateTimeOffset(2026, 5, 25, 10, 0, 0, TimeSpan.Zero),
                OffSaleDate = new DateTimeOffset(2026, 6, 1, 18, 0, 0, TimeSpan.Zero)
            });
            seedContext.BundleEventSchedules.Add(new BundleEventSchedule
            {
                BundleId = 20,
                EventScheduleId = 10
            });
            await seedContext.SaveChangesAsync();
        }

        var seatsIo = Substitute.For<ISeatsIoSeasonLifecycleClient>();
        seatsIo.CreateSeatsIoEventsInSeasonAsync("season-20", Arg.Any<string[]>())
            .Returns(_ =>
            {
                connection.Close();
                return Task.CompletedTask;
            });

        await using var context = new XBOLDbContext(options);
        var sut = new AddEventsToSeasonHandler(
            new BundleRepository(context),
            seatsIo,
            Substitute.For<IBundlePassTicketMaterializationService>(),
            NullLogger<AddEventsToSeasonHandler>.Instance);

        var act = () => sut.Handle(new AddEventsToSeasonCommand(20, [10]));

        await act.Should().ThrowAsync<Exception>();
        await seatsIo.Received(1).DeleteSeatsIoEventAsync("season-20-schedule-10");
    }

    [Fact]
    public async Task DeleteSeatsIoSeasonHandler_DeletesSeasonAndClearsOwnedLocalKeys()
    {
        var userId = Guid.NewGuid();
        var bundle = SeasonPassBundle(20,
        [
            ScheduleLink(20, 10, "season-20-schedule-10"),
            ScheduleLink(20, 11, "schedule-11")
        ]);
        bundle.Status = EventStatus.Published;
        bundle.ExternalKey = "season-20";
        _bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(20).Returns(bundle);

        var sut = DeleteSeasonHandler();

        await sut.Handle(new DeleteSeatsIoSeasonCommand(20, userId));

        await _seatsIo.Received(1).DeleteSeatsIoSeasonAsync("season-20");
        bundle.ExternalKey.Should().BeNull();
        bundle.Status.Should().Be(EventStatus.Cancelled);
        bundle.UpdatedBy.Should().Be(userId);
        bundle.BundleEventSchedules[0].EventSchedule.ExternalEventKey.Should().BeNull();
        bundle.BundleEventSchedules[1].EventSchedule.ExternalEventKey.Should().Be("schedule-11");
        await _bundleRepository.Received(1).UpdateAsync(bundle);
    }

    [Fact]
    public async Task DeleteSeatsIoSeasonHandler_RemoteDeleteFailurePreservesLocalState()
    {
        var bundle = SeasonPassBundle(20, [ScheduleLink(20, 10, "season-20-schedule-10")]);
        bundle.Status = EventStatus.Published;
        bundle.ExternalKey = "season-20";
        _bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(20).Returns(bundle);
        _seatsIo.DeleteSeatsIoSeasonAsync("season-20")
            .Returns(Task.FromException(new InvalidOperationException("seats unavailable")));

        var sut = DeleteSeasonHandler();

        var act = () => sut.Handle(new DeleteSeatsIoSeasonCommand(20, Guid.Empty));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("seats unavailable");
        bundle.ExternalKey.Should().Be("season-20");
        bundle.Status.Should().Be(EventStatus.Published);
        bundle.BundleEventSchedules[0].EventSchedule.ExternalEventKey.Should()
            .Be("season-20-schedule-10");
        await _bundleRepository.DidNotReceive().UpdateAsync(bundle);
    }

    [Fact]
    public async Task UpdateSeatsIoSeasonHandler_UpdatesSeasonName()
    {
        var bundle = SeasonPassBundle(20, []);
        bundle.Status = EventStatus.Published;
        bundle.ExternalKey = "season-20";
        bundle.Name = "Updated Season";
        _bundleRepository.GetByIdAsync(20).Returns(bundle);

        var sut = UpdateSeasonHandler();

        await sut.Handle(new UpdateSeatsIoSeasonCommand(20));

        await _seatsIo.Received(1).UpdateSeatsIoSeasonAsync("season-20", "Updated Season");
    }

    [Fact]
    public async Task UpdateSeatsIoSeasonHandler_RemoteFailureDoesNotBlockLocalUpdate()
    {
        var bundle = SeasonPassBundle(20, []);
        bundle.Status = EventStatus.Published;
        bundle.ExternalKey = "season-20";
        bundle.Name = "Updated Season";
        _bundleRepository.GetByIdAsync(20).Returns(bundle);
        _seatsIo.UpdateSeatsIoSeasonAsync("season-20", "Updated Season")
            .Returns(Task.FromException(new InvalidOperationException("seats unavailable")));

        var sut = UpdateSeasonHandler();

        var act = () => sut.Handle(new UpdateSeatsIoSeasonCommand(20));

        await act.Should().NotThrowAsync();
    }

    private CreateSeatsIoSeasonHandler Handler()
    {
        return new CreateSeatsIoSeasonHandler(
            _bundleRepository,
            _seatsIo,
            _ticketMaterializer,
            NullLogger<CreateSeatsIoSeasonHandler>.Instance);
    }

    private AddEventsToSeasonHandler AddEventsHandler()
    {
        return new AddEventsToSeasonHandler(
            _bundleRepository,
            _seatsIo,
            _ticketMaterializer,
            NullLogger<AddEventsToSeasonHandler>.Instance);
    }

    private DeleteSeatsIoSeasonHandler DeleteSeasonHandler()
    {
        return new DeleteSeatsIoSeasonHandler(
            _bundleRepository,
            _seatsIo,
            NullLogger<DeleteSeatsIoSeasonHandler>.Instance);
    }

    private UpdateSeatsIoSeasonHandler UpdateSeasonHandler()
    {
        return new UpdateSeatsIoSeasonHandler(
            _bundleRepository,
            _seatsIo,
            NullLogger<UpdateSeatsIoSeasonHandler>.Instance);
    }

    private static Bundle SeasonPassBundle(long id, List<BundleEventSchedule> links)
    {
        return new Bundle
        {
            Id = id,
            VenueMapId = 2,
            VenueMap = new VenueMap
            {
                Id = 2,
                Name = "Main chart",
                ExternalMapKey = "chart-main"
            },
            Name = "Season Pass",
            BundleType = BundleType.SeasonPass,
            BundlePricingType = BundlePricingType.Composite,
            Status = EventStatus.Approved,
            BundleEventSchedules = links
        };
    }

    private static BundleEventSchedule ScheduleLink(
        long bundleId,
        long scheduleId,
        string? externalEventKey = null)
    {
        return new BundleEventSchedule
        {
            BundleId = bundleId,
            EventScheduleId = scheduleId,
            EventSchedule = new EventSchedule
            {
                Id = scheduleId,
                EventId = 1,
                ExternalEventKey = externalEventKey,
                Status = ScheduleStatus.Draft,
                StartDateTime = new DateTimeOffset(2026, 6, 1, 19, 0, 0, TimeSpan.Zero),
                EndDateTime = new DateTimeOffset(2026, 6, 1, 22, 0, 0, TimeSpan.Zero),
                OnSaleDate = new DateTimeOffset(2026, 5, 25, 10, 0, 0, TimeSpan.Zero),
                OffSaleDate = new DateTimeOffset(2026, 6, 1, 18, 0, 0, TimeSpan.Zero)
            }
        };
    }
}
