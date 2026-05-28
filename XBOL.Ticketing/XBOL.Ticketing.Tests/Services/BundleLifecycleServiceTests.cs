using FluentAssertions;
using NSubstitute;
using Wolverine;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Services.Bundle;
using XBOL.Ticketing.Services.Event;
using XBOL.Ticketing.Services.Messages;

namespace XBOL.Ticketing.Tests.Services;

public class BundleLifecycleServiceTests
{
    private readonly IBundleRepository _bundleRepository = Substitute.For<IBundleRepository>();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly IEventScheduleLifecycleService _eventScheduleLifecycleService =
        Substitute.For<IEventScheduleLifecycleService>();
    private readonly BundleLifecycleService _sut;

    public BundleLifecycleServiceTests()
    {
        _bus.InvokeAsync(Arg.Any<object>(), Arg.Any<CancellationToken>(), Arg.Any<TimeSpan?>())
            .Returns(Task.CompletedTask);

        _sut = new BundleLifecycleService(
            _bundleRepository,
            _bus,
            _eventScheduleLifecycleService);
    }

    [Fact]
    public async Task PublishAsync_SeasonPassBundle_InvokesCreateSeasonCommand()
    {
        _bundleRepository.GetByIdAsync(20)
            .Returns(Bundle(20, BundleType.SeasonPass, EventStatus.Approved));

        await _sut.PublishAsync(20, Guid.Empty);

        await _bus.Received(1).InvokeAsync(
            Arg.Is<CreateSeatsIoSeasonCommand>(command => command.BundleId == 20),
            Arg.Any<CancellationToken>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task PublishAsync_NonSeasonPassBundle_DoesNotCreateRemoteContainer()
    {
        var bundle = Bundle(20, BundleType.Basic, EventStatus.Approved);
        _bundleRepository.GetByIdAsync(20).Returns(bundle);

        await _sut.PublishAsync(20, Guid.Empty);

        await _bus.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default, default);
        bundle.Status.Should().Be(EventStatus.Published);
        bundle.ExternalKey.Should().BeNull();
        await _bundleRepository.Received(1).UpdateAsync(bundle);
    }

    [Fact]
    public async Task PublishAsync_BasicBundle_PublishesMissingLinkedSchedules()
    {
        var userId = Guid.NewGuid();
        var bundle = Bundle(20, BundleType.Basic, EventStatus.Approved, "legacy-container");
        bundle.BundleEventSchedules =
        [
            ScheduleLink(20, 10),
            ScheduleLink(20, 11, "schedule-11")
        ];
        _bundleRepository.GetByIdAsync(20).Returns(bundle);
        _bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(20).Returns(bundle);

        await _sut.PublishAsync(20, userId);

        await _eventScheduleLifecycleService.Received(1).PublishAsync(
            10,
            userId,
            Arg.Any<CancellationToken>());
        await _eventScheduleLifecycleService.DidNotReceive().PublishAsync(
            11,
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await _bus.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default, default);
        bundle.Status.Should().Be(EventStatus.Published);
        bundle.ExternalKey.Should().BeNull();
        await _bundleRepository.Received(1).UpdateAsync(bundle);
    }

    [Fact]
    public async Task AddSchedulesAsync_PublishedSeasonPass_InvokesAddEventsCommand()
    {
        _bundleRepository.GetByIdAsync(20)
            .Returns(Bundle(20, BundleType.SeasonPass, EventStatus.Published, "season-20"));

        await _sut.AddSchedulesAsync(20, [10, 11]);

        await _bus.Received(1).InvokeAsync(
            Arg.Is<AddEventsToSeasonCommand>(command =>
                command.BundleId == 20 &&
                command.EventScheduleIds.SequenceEqual(new[] { 10L, 11L })),
            Arg.Any<CancellationToken>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task AddSchedulesAsync_SeasonPassWithoutExternalKey_RejectsAdd()
    {
        _bundleRepository.GetByIdAsync(20)
            .Returns(Bundle(20, BundleType.SeasonPass, EventStatus.Published));

        var act = () => _sut.AddSchedulesAsync(20, [10]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*has no Seats.io season key*");
        await _bus.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default, default);
    }

    [Fact]
    public async Task AddSchedulesAsync_PublishedBasicBundle_PublishesMissingScheduleKeys()
    {
        var bundle = Bundle(20, BundleType.Basic, EventStatus.Published, "legacy-container");
        bundle.BundleEventSchedules =
        [
            ScheduleLink(20, 10),
            ScheduleLink(20, 11, "schedule-11"),
            ScheduleLink(20, 12)
        ];
        _bundleRepository.GetByIdAsync(20).Returns(bundle);
        _bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(20).Returns(bundle);

        await _sut.AddSchedulesAsync(20, [10, 11]);

        await _eventScheduleLifecycleService.Received(1).PublishAsync(
            10,
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await _eventScheduleLifecycleService.DidNotReceive().PublishAsync(
            11,
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await _eventScheduleLifecycleService.DidNotReceive().PublishAsync(
            12,
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await _bus.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default, default);
        bundle.ExternalKey.Should().BeNull();
        await _bundleRepository.Received(1).UpdateAsync(bundle);
    }

    [Fact]
    public async Task SyncMetadataAsync_PublishedSeasonPass_InvokesUpdateSeasonCommand()
    {
        _bundleRepository.GetByIdAsync(20)
            .Returns(Bundle(20, BundleType.SeasonPass, EventStatus.Published, "season-20"));

        await _sut.SyncMetadataAsync(20);

        await _bus.Received(1).InvokeAsync(
            Arg.Is<UpdateSeatsIoSeasonCommand>(command => command.BundleId == 20),
            Arg.Any<CancellationToken>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task SyncMetadataAsync_BasicBundle_DoesNotInvokeRemoteUpdate()
    {
        _bundleRepository.GetByIdAsync(20)
            .Returns(Bundle(20, BundleType.Basic, EventStatus.Published));

        await _sut.SyncMetadataAsync(20);

        await _bus.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default, default);
    }

    [Fact]
    public async Task CancelAsync_PublishedSeasonPass_InvokesDeleteSeasonCommand()
    {
        var userId = Guid.NewGuid();
        _bundleRepository.GetByIdAsync(20)
            .Returns(Bundle(20, BundleType.SeasonPass, EventStatus.Published, "season-20"));

        await _sut.CancelAsync(20, userId);

        await _bus.Received(1).InvokeAsync(
            Arg.Is<DeleteSeatsIoSeasonCommand>(command =>
                command.BundleId == 20 &&
                command.UserId == userId),
            Arg.Any<CancellationToken>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task CancelAsync_NonSeasonPassBundle_DoesNotDeleteRemoteContainer()
    {
        var userId = Guid.NewGuid();
        var bundle = Bundle(20, BundleType.Basic, EventStatus.Published, "legacy-container");
        _bundleRepository.GetByIdAsync(20).Returns(bundle);

        await _sut.CancelAsync(20, userId);

        await _bus.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default, default);
        await _bundleRepository.DidNotReceive().GetByIdWithVenueMapAndSchedulesAsync(20);
        bundle.Status.Should().Be(EventStatus.Cancelled);
        bundle.ExternalKey.Should().BeNull();
        bundle.UpdatedBy.Should().Be(userId);
        await _bundleRepository.Received(1).UpdateAsync(bundle);
    }

    private static Bundle Bundle(
        long id,
        BundleType bundleType,
        EventStatus status,
        string? externalKey = null)
    {
        return new Bundle
        {
            Id = id,
            Name = $"{bundleType} Bundle",
            BundleType = bundleType,
            BundlePricingType = BundlePricingType.Composite,
            Status = status,
            ExternalKey = externalKey
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
                ExternalEventKey = externalEventKey
            }
        };
    }
}
