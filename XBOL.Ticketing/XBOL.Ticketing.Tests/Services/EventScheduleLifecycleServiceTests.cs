using FluentAssertions;
using NSubstitute;
using Wolverine;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Services.Event;
using XBOL.Ticketing.Services.Messages;

namespace XBOL.Ticketing.Tests.Services;

public class EventScheduleLifecycleServiceTests
{
    private readonly IEventScheduleRepository _eventScheduleRepository = Substitute.For<IEventScheduleRepository>();
    private readonly IBundleEventScheduleRepository _bundleEventScheduleRepository = Substitute.For<IBundleEventScheduleRepository>();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly EventScheduleLifecycleService _sut;

    public EventScheduleLifecycleServiceTests()
    {
        _bus.InvokeAsync(Arg.Any<object>(), Arg.Any<CancellationToken>(), Arg.Any<TimeSpan?>())
            .Returns(Task.CompletedTask);

        _sut = new EventScheduleLifecycleService(
            _eventScheduleRepository,
            _bundleEventScheduleRepository,
            _bus);
    }

    [Fact]
    public async Task PublishAsync_SeasonPassLinkedSchedule_InvokesAddEventsToSeasonCommand()
    {
        var userId = Guid.NewGuid();
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(10)
            .Returns(Schedule(10, ScheduleStatus.Draft));
        _bundleEventScheduleRepository.GetByEventScheduleIdAsync(10).Returns(
        [
            new BundleEventSchedule
            {
                BundleId = 20,
                EventScheduleId = 10,
                Bundle = new Core.Model.Bundle
                {
                    Id = 20,
                    BundleType = BundleType.SeasonPass,
                    Status = EventStatus.Published,
                    ExternalKey = "season-20"
                }
            }
        ]);

        await _sut.PublishAsync(10, userId);

        await _bus.Received(1).InvokeAsync(
            Arg.Is<AddEventsToSeasonCommand>(command =>
                command.BundleId == 20 &&
                command.EventScheduleIds.SequenceEqual(new[] { 10L }) &&
                command.UserId == userId),
            Arg.Any<CancellationToken>(),
            Arg.Any<TimeSpan?>());
        await _eventScheduleRepository.DidNotReceive().UpdateAsync(Arg.Any<EventSchedule>());
    }

    [Fact]
    public async Task PublishAsync_BasicLinkedSchedule_InvokesStandaloneCreateCommand()
    {
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(10)
            .Returns(Schedule(10, ScheduleStatus.Draft));
        _bundleEventScheduleRepository.GetByEventScheduleIdAsync(10).Returns(
        [
            new BundleEventSchedule
            {
                BundleId = 20,
                Bundle = new Core.Model.Bundle { Id = 20, BundleType = BundleType.Basic }
            }
        ]);

        await _sut.PublishAsync(10, Guid.Empty);

        await _bus.Received(1).InvokeAsync(
            Arg.Is<CreateSeatsIoEventCommand>(command => command.EventScheduleId == 10),
            Arg.Any<CancellationToken>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task CancelAsync_BasicLinkedSchedule_InvokesStandaloneDeleteCommand()
    {
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(10)
            .Returns(Schedule(10, ScheduleStatus.OnSale, "schedule-10"));
        _bundleEventScheduleRepository.GetByEventScheduleIdAsync(10).Returns(
        [
            new BundleEventSchedule
            {
                BundleId = 20,
                Bundle = new Core.Model.Bundle { Id = 20, BundleType = BundleType.Basic }
            }
        ]);

        await _sut.CancelAsync(10, Guid.Empty);

        await _bus.Received(1).InvokeAsync(
            Arg.Is<DeleteSeatsIoEventCommand>(command =>
                command.EventScheduleId == 10 &&
                command.Mode == SeatsIoEventDeletionMode.Close),
            Arg.Any<CancellationToken>(),
            Arg.Any<TimeSpan?>());
    }

    private static EventSchedule Schedule(long id, ScheduleStatus status, string? externalEventKey = null)
    {
        return new EventSchedule
        {
            Id = id,
            EventId = 1,
            Status = status,
            ExternalEventKey = externalEventKey,
            StartDateTime = new DateTimeOffset(2026, 6, 1, 19, 0, 0, TimeSpan.Zero),
            EndDateTime = new DateTimeOffset(2026, 6, 1, 22, 0, 0, TimeSpan.Zero),
            OnSaleDate = new DateTimeOffset(2026, 5, 25, 10, 0, 0, TimeSpan.Zero),
            OffSaleDate = new DateTimeOffset(2026, 6, 1, 18, 0, 0, TimeSpan.Zero),
            Event = new Event
            {
                Id = 1,
                Name = "Opening Match",
                Status = EventStatus.Approved,
                VenueMapId = 2,
                VenueMap = new VenueMap
                {
                    Id = 2,
                    Name = "Main chart",
                    ExternalMapKey = "chart-main"
                }
            }
        };
    }
}
