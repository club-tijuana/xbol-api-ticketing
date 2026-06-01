using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SeatsioDotNet;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Services;
using XBOL.Ticketing.Services.Event;
using XBOL.Ticketing.Services.Messages;

namespace XBOL.Ticketing.Tests.Services;

public class EventScheduleSeatsIoHandlerTests
{
    private readonly IEventScheduleRepository _eventScheduleRepository = Substitute.For<IEventScheduleRepository>();
    private readonly ISeatsIoEventLifecycleClient _seatsIo = Substitute.For<ISeatsIoEventLifecycleClient>();

    [Fact]
    public async Task CreateSeatsIoEventHandler_CreatesRemoteEventAndPersistsExternalEventKey()
    {
        var schedule = Schedule(10, ScheduleStatus.Draft);
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(10).Returns(schedule);

        var sut = new CreateSeatsIoEventHandler(
            _eventScheduleRepository,
            _seatsIo,
            NullLogger<CreateSeatsIoEventHandler>.Instance);

        await sut.Handle(new CreateSeatsIoEventCommand(10, Guid.Empty));

        await _seatsIo.Received(1).CreateSeatsIoEventAsync(
            "chart-main",
            "schedule-10",
            "Opening Match",
            new DateOnly(2026, 6, 1));
        schedule.ExternalEventKey.Should().Be("schedule-10");
        schedule.Status.Should().Be(ScheduleStatus.OnSale);
        schedule.PublishedDate.Should().NotBeNull();
        await _eventScheduleRepository.Received(1).UpdateAsync(schedule);
    }

    [Fact]
    public async Task DeleteSeatsIoEventHandler_CloseModeDeletesRemoteEventAndClearsExternalEventKey()
    {
        var schedule = Schedule(10, ScheduleStatus.OnSale, "schedule-10");
        _eventScheduleRepository.GetByIdIncludingDeletedAsync(10).Returns(schedule);

        var sut = DeleteHandler();

        await sut.Handle(new DeleteSeatsIoEventCommand(10, Guid.Empty, SeatsIoEventDeletionMode.Close));

        await _seatsIo.Received(1).DeleteSeatsIoEventAsync("schedule-10");
        schedule.ExternalEventKey.Should().BeNull();
        schedule.Status.Should().Be(ScheduleStatus.Closed);
        schedule.DeletedAt.Should().BeNull();
        await _eventScheduleRepository.Received(1).UpdateAsync(schedule);
    }

    [Fact]
    public async Task DeleteSeatsIoEventHandler_CloseModeValidatesBeforeDeletingRemoteEvent()
    {
        var schedule = Schedule(10, ScheduleStatus.Completed, "schedule-10");
        _eventScheduleRepository.GetByIdIncludingDeletedAsync(10).Returns(schedule);

        var sut = DeleteHandler();

        var act = () => sut.Handle(new DeleteSeatsIoEventCommand(10, Guid.Empty, SeatsIoEventDeletionMode.Close));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid schedule status transition from 'Completed' to 'Closed'.");
        await _seatsIo.DidNotReceive().DeleteSeatsIoEventAsync("schedule-10");
        schedule.ExternalEventKey.Should().Be("schedule-10");
        schedule.Status.Should().Be(ScheduleStatus.Completed);
        schedule.DeletedAt.Should().BeNull();
        await _eventScheduleRepository.DidNotReceive().UpdateAsync(schedule);
    }

    [Theory]
    [InlineData(SeatsIoEventDeletionMode.Close)]
    [InlineData(SeatsIoEventDeletionMode.SoftDelete)]
    public async Task DeleteSeatsIoEventHandler_DeleteFailurePreservesLocalState(SeatsIoEventDeletionMode mode)
    {
        var schedule = Schedule(10, ScheduleStatus.OnSale, "schedule-10");
        _eventScheduleRepository.GetByIdIncludingDeletedAsync(10).Returns(schedule);
        _seatsIo.DeleteSeatsIoEventAsync("schedule-10")
            .Returns(Task.FromException(new TestSeatsioException("RATE_LIMIT_EXCEEDED")));

        var sut = DeleteHandler();

        var act = () => sut.Handle(new DeleteSeatsIoEventCommand(10, Guid.Empty, mode));

        await act.Should().ThrowAsync<TestSeatsioException>();
        schedule.ExternalEventKey.Should().Be("schedule-10");
        schedule.Status.Should().Be(ScheduleStatus.OnSale);
        schedule.DeletedAt.Should().BeNull();
        await _eventScheduleRepository.DidNotReceive().UpdateAsync(schedule);
    }

    [Theory]
    [InlineData(SeatsIoEventDeletionMode.Close)]
    [InlineData(SeatsIoEventDeletionMode.SoftDelete)]
    public async Task DeleteSeatsIoEventHandler_RemoteNotFoundClearsExternalEventKey(SeatsIoEventDeletionMode mode)
    {
        var schedule = Schedule(10, ScheduleStatus.OnSale, "schedule-10");
        _eventScheduleRepository.GetByIdIncludingDeletedAsync(10).Returns(schedule);
        _seatsIo.DeleteSeatsIoEventAsync("schedule-10")
            .Returns(Task.FromException(new TestSeatsioException("EVENT_NOT_FOUND")));

        var sut = DeleteHandler();

        await sut.Handle(new DeleteSeatsIoEventCommand(10, Guid.Empty, mode));

        schedule.ExternalEventKey.Should().BeNull();
        if (mode == SeatsIoEventDeletionMode.Close)
        {
            schedule.Status.Should().Be(ScheduleStatus.Closed);
            schedule.DeletedAt.Should().BeNull();
        }
        else
        {
            schedule.Status.Should().Be(ScheduleStatus.OnSale);
            schedule.DeletedAt.Should().NotBeNull();
        }

        await _eventScheduleRepository.Received(1).UpdateAsync(schedule);
    }

    private DeleteSeatsIoEventHandler DeleteHandler()
    {
        return new DeleteSeatsIoEventHandler(
            _eventScheduleRepository,
            _seatsIo,
            NullLogger<DeleteSeatsIoEventHandler>.Instance);
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

    private sealed class TestSeatsioException(string code)
        : SeatsioException([new SeatsioApiError(code, "Seats.io error.")], "request-1");
}
