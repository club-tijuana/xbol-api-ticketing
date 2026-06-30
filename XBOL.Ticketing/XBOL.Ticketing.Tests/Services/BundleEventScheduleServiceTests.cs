using FluentAssertions;
using NSubstitute;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Services.Bundle;

namespace XBOL.Ticketing.Tests.Services;

    public class BundleEventScheduleServiceTests
    {
        private const long VenueMapId = 1;
        private static readonly DateTimeOffset FutureStart = DateTimeOffset.UtcNow.AddDays(30);

    private readonly IBundleEventScheduleRepository _scheduleRepo = Substitute.For<IBundleEventScheduleRepository>();
    private readonly IBundleRepository _bundleRepo = Substitute.For<IBundleRepository>();
    private readonly IEventScheduleRepository _eventScheduleRepo = Substitute.For<IEventScheduleRepository>();
    private readonly IBundleLifecycleService _bundleLifecycleService = Substitute.For<IBundleLifecycleService>();
    private readonly BundleEventScheduleService _sut;

    public BundleEventScheduleServiceTests()
    {
        _sut = new BundleEventScheduleService(
            _scheduleRepo,
            _bundleRepo,
            _eventScheduleRepo,
            _bundleLifecycleService);
    }

    private static Core.Model.Bundle Bundle(
        EventStatus status,
        BundleType type = BundleType.Basic,
        string? externalKey = null) => new()
    {
        Id = 1,
        Status = status,
        BundleType = type,
        ExternalKey = externalKey,
        VenueMapId = VenueMapId
    };

    private static EventSchedule Schedule(
        long id,
        string? extKey = null,
        ScheduleStatus status = ScheduleStatus.Draft,
        EventStatus eventStatus = EventStatus.PendingReview,
        DateTimeOffset? startDateTime = null,
        DateTimeOffset? deletedAt = null) => new()
    {
        Id = id,
        ExternalEventKey = extKey,
        Status = status,
        StartDateTime = startDateTime ?? FutureStart,
        DeletedAt = deletedAt,
        Event = new Core.Model.Event
        {
            VenueMapId = VenueMapId,
            Status = eventStatus
        }
    };

    [Fact]
    public async Task AddAsync_CancelledStatus_BlocksModifications()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Cancelled));

        var act = () => _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*status*Draft*PendingReview*Approved*");
    }

    [Theory]
    [InlineData(EventStatus.Draft)]
    [InlineData(EventStatus.PendingReview)]
    [InlineData(EventStatus.Approved)]
    public async Task AddAsync_EditableStatus_AllowsModifications(EventStatus status)
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(status));
        _eventScheduleRepo.GetByIdWithEventAndVenueMapAsync(10).Returns(Schedule(10));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);
        _scheduleRepo.GetByEventScheduleIdAsync(10).Returns([]);
        _scheduleRepo.GetByBundleIdWithSchedulesAsync(1).Returns(new List<BundleEventSchedule>());

        await _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await _scheduleRepo.Received(1).InsertAsync(Arg.Any<BundleEventSchedule>());
    }

    [Fact]
    public async Task AddAsync_PublishedSeasonPass_AddsScheduleWithoutInvokingLifecycle()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Published, BundleType.SeasonPass, "season-1"));
        _eventScheduleRepo.GetByIdWithEventAndVenueMapAsync(10).Returns(Schedule(10));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);
        _scheduleRepo.GetByEventScheduleIdAsync(10).Returns([]);
        _scheduleRepo.GetByBundleIdWithSchedulesAsync(1).Returns(new List<BundleEventSchedule>());

        await _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await _scheduleRepo.Received(1).InsertAsync(Arg.Is<BundleEventSchedule>(link =>
            link.BundleId == 1 &&
            link.EventScheduleId == 10));
        await _scheduleRepo.Received(1).CommitAsync();
        await _bundleLifecycleService.DidNotReceiveWithAnyArgs().AddSchedulesAsync(
            default,
            default!,
            default);
    }

    [Fact]
    public async Task AddAsync_PublishedBasicBundle_AddsScheduleAndInvokesLifecycle()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Published, BundleType.Basic));
        _eventScheduleRepo.GetByIdWithEventAndVenueMapAsync(10).Returns(Schedule(10));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);
        _scheduleRepo.GetByEventScheduleIdAsync(10).Returns([]);
        _scheduleRepo.GetByBundleIdWithSchedulesAsync(1).Returns(new List<BundleEventSchedule>());

        await _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await _scheduleRepo.Received(1).InsertAsync(Arg.Is<BundleEventSchedule>(link =>
            link.BundleId == 1 &&
            link.EventScheduleId == 10));
        await _bundleLifecycleService.Received(1).AddSchedulesAsync(
            1,
            Arg.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new[] { 10L })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_SeasonPass_RejectsScheduleWithExternalEventKey()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Draft, BundleType.SeasonPass));
        _eventScheduleRepo.GetByIdWithEventAndVenueMapAsync(10).Returns(Schedule(10, "already-synced-event"));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);

        var act = () => _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ExternalEventKey*born inside a Seats.io season*");
    }

    [Fact]
    public async Task AddAsync_SeasonPass_RejectsScheduleBelongingToAnotherSeasonPass()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Draft, BundleType.SeasonPass));
        _eventScheduleRepo.GetByIdWithEventAndVenueMapAsync(10).Returns(Schedule(10));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);
        _scheduleRepo.GetByEventScheduleIdAsync(10).Returns(
        [
            new BundleEventSchedule
            {
                BundleId = 2,
                Bundle = new Core.Model.Bundle { Id = 2, BundleType = BundleType.SeasonPass }
            }
        ]);

        var act = () => _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*another SeasonPass*1-season-parent*");
    }

    [Fact]
    public async Task AddAsync_BasicBundle_IgnoresSeasonPassConstraints()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Draft, BundleType.Basic));
        _eventScheduleRepo.GetByIdWithEventAndVenueMapAsync(10).Returns(Schedule(10, "already-synced-event", ScheduleStatus.OnSale));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);
        _scheduleRepo.GetByEventScheduleIdAsync(10).Returns(
        [
            new BundleEventSchedule
            {
                BundleId = 2,
                Bundle = new Core.Model.Bundle { Id = 2, BundleType = BundleType.SeasonPass }
            }
        ]);
        _scheduleRepo.GetByBundleIdWithSchedulesAsync(1).Returns(new List<BundleEventSchedule>());

        await _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await _scheduleRepo.Received(1).InsertAsync(Arg.Any<BundleEventSchedule>());
    }

    [Fact]
    public async Task AddAsync_RejectsPastSchedule()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Draft, BundleType.Basic));
        _eventScheduleRepo.GetByIdWithEventAndVenueMapAsync(10)
            .Returns(Schedule(10, startDateTime: DateTimeOffset.UtcNow.AddDays(-1)));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);

        var act = () => _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*past*");
    }

    [Fact]
    public async Task AddAsync_RejectsDeletedSchedule()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Draft, BundleType.Basic));
        _eventScheduleRepo.GetByIdWithEventAndVenueMapAsync(10)
            .Returns(Schedule(10, deletedAt: DateTimeOffset.UtcNow));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);

        var act = () => _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*deleted*");
    }

    [Theory]
    [InlineData(ScheduleStatus.Closed)]
    [InlineData(ScheduleStatus.Completed)]
    public async Task AddAsync_RejectsInactiveSchedules(ScheduleStatus status)
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Draft, BundleType.Basic));
        _eventScheduleRepo.GetByIdWithEventAndVenueMapAsync(10)
            .Returns(Schedule(10, status: status));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);

        var act = () => _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{status}*");
    }

    [Theory]
    [InlineData(EventStatus.Draft)]
    [InlineData(EventStatus.ChangesRequested)]
    [InlineData(EventStatus.Cancelled)]
    public async Task AddAsync_RejectsSchedulesWhoseParentEventIsNotSelectable(EventStatus status)
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Draft, BundleType.Basic));
        _eventScheduleRepo.GetByIdWithEventAndVenueMapAsync(10)
            .Returns(Schedule(10, eventStatus: status));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);

        var act = () => _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{status}*");
    }

    [Fact]
    public async Task AddAsync_BasicBundleRejectsOnSaleScheduleWithoutExternalEventKey()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Draft, BundleType.Basic));
        _eventScheduleRepo.GetByIdWithEventAndVenueMapAsync(10)
            .Returns(Schedule(10, status: ScheduleStatus.OnSale));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);

        var act = () => _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*OnSale*ExternalEventKey*");
    }

    [Fact]
    public async Task AddAsync_SeasonPassRejectsNonDraftSchedules()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Draft, BundleType.SeasonPass));
        _eventScheduleRepo.GetByIdWithEventAndVenueMapAsync(10)
            .Returns(Schedule(10, status: ScheduleStatus.OnSale));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);

        var act = () => _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SeasonPass*Draft*");
    }

    [Fact]
    public async Task AddAsync_SeasonPass_SameBundleLink_DoesNotTrigger1SeasonConstraint()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Draft, BundleType.SeasonPass));
        _eventScheduleRepo.GetByIdWithEventAndVenueMapAsync(10).Returns(Schedule(10));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);
        _scheduleRepo.GetByEventScheduleIdAsync(10).Returns(
        [
            new BundleEventSchedule
            {
                BundleId = 1,
                Bundle = new Core.Model.Bundle { Id = 1, BundleType = BundleType.SeasonPass }
            }
        ]);

        var act = () => _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await act.Should().NotThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RemoveAsync_StatusGate_BlocksNonEditable()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Cancelled));

        var act = () => _sut.RemoveAsync(1, 10);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*status*");
    }

    [Fact]
    public async Task RemoveAsync_PublishedBasicBundle_RemovesLocalMembership()
    {
        var entry = new BundleEventSchedule { BundleId = 1, EventScheduleId = 10 };
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Published, BundleType.Basic));
        _scheduleRepo.GetByCompositeKeyAsync(1, 10).Returns(entry);

        var result = await _sut.RemoveAsync(1, 10);

        result.Should().BeTrue();
        _scheduleRepo.Received(1).Remove(entry);
        await _scheduleRepo.Received(1).CommitAsync();
    }

    [Fact]
    public async Task RemoveAsync_PublishedSeasonPass_BlocksModifications()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Published, BundleType.SeasonPass, "season-1"));

        var act = () => _sut.RemoveAsync(1, 10);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*status*");
    }

    [Fact]
    public async Task RemoveBatchAsync_OnlyCountsActuallyRemoved()
    {
        var entry = new BundleEventSchedule { BundleId = 1, EventScheduleId = 10 };
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Draft));
        _scheduleRepo.GetByCompositeKeyAsync(1, 10).Returns(entry);
        _scheduleRepo.GetByCompositeKeyAsync(1, 20).Returns((BundleEventSchedule?)null);

        var result = await _sut.RemoveBatchAsync(1, new BundleEventScheduleRemoveRequest
        {
            EventScheduleIds = [10, 20]
        });

        result.Should().Be(1);
    }
}
