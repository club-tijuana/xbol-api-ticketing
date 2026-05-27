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
    private readonly IBundleEventScheduleRepository _scheduleRepo = Substitute.For<IBundleEventScheduleRepository>();
    private readonly IBundleRepository _bundleRepo = Substitute.For<IBundleRepository>();
    private readonly IEventScheduleRepository _eventScheduleRepo = Substitute.For<IEventScheduleRepository>();
    private readonly BundleEventScheduleService _sut;

    public BundleEventScheduleServiceTests()
    {
        _sut = new BundleEventScheduleService(_scheduleRepo, _bundleRepo, _eventScheduleRepo);
    }

    private static Core.Model.Bundle Bundle(EventStatus status, BundleType type = BundleType.Basic) => new()
    {
        Id = 1, Status = status, BundleType = type
    };

    private static EventSchedule Schedule(long id, string? extKey = null) => new()
    {
        Id = id, ExternalEventKey = extKey
    };

    [Theory]
    [InlineData(EventStatus.Published)]
    [InlineData(EventStatus.Cancelled)]
    public async Task AddAsync_NonEditableStatus_BlocksModifications(EventStatus status)
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(status));

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
        _eventScheduleRepo.GetByIdAsync(10).Returns(Schedule(10));
        _scheduleRepo.ExistsAsync(1, 10).Returns(false);
        _scheduleRepo.GetByBundleIdWithSchedulesAsync(1).Returns(new List<BundleEventSchedule>());

        await _sut.AddAsync(1, new BundleEventScheduleAddRequest
        {
            Items = [new() { EventScheduleId = 10, SortOrder = 1 }]
        });

        await _scheduleRepo.Received(1).InsertAsync(Arg.Any<BundleEventSchedule>());
    }

    [Fact]
    public async Task AddAsync_SeasonPass_RejectsScheduleWithExternalEventKey()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Draft, BundleType.SeasonPass));
        _eventScheduleRepo.GetByIdAsync(10).Returns(Schedule(10, "already-synced-event"));
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
        _eventScheduleRepo.GetByIdAsync(10).Returns(Schedule(10));
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
        _eventScheduleRepo.GetByIdAsync(10).Returns(Schedule(10, "already-synced-event"));
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
    public async Task AddAsync_SeasonPass_SameBundleLink_DoesNotTrigger1SeasonConstraint()
    {
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Draft, BundleType.SeasonPass));
        _eventScheduleRepo.GetByIdAsync(10).Returns(Schedule(10));
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
        _bundleRepo.GetByIdAsync(1).Returns(Bundle(EventStatus.Published));

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
