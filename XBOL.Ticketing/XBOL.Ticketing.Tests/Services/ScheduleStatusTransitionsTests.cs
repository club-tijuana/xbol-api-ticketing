using FluentAssertions;
using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Tests.Services;

public class ScheduleStatusTransitionsTests
{
    [Theory]
    [InlineData(ScheduleStatus.Draft, ScheduleStatus.OnSale)]
    [InlineData(ScheduleStatus.Draft, ScheduleStatus.Closed)]
    [InlineData(ScheduleStatus.OnSale, ScheduleStatus.Closed)]
    [InlineData(ScheduleStatus.OnSale, ScheduleStatus.Completed)]
    [InlineData(ScheduleStatus.Closed, ScheduleStatus.Completed)]
    public void IsValidTransition_ValidTransitions_ReturnsTrue(ScheduleStatus from, ScheduleStatus to)
    {
        ScheduleStatusTransitions.IsValidTransition(from, to).Should().BeTrue();
    }

    [Theory]
    [InlineData(ScheduleStatus.Draft, ScheduleStatus.Completed)]
    [InlineData(ScheduleStatus.OnSale, ScheduleStatus.Draft)]
    [InlineData(ScheduleStatus.OnSale, ScheduleStatus.OnSale)]
    [InlineData(ScheduleStatus.Closed, ScheduleStatus.Draft)]
    [InlineData(ScheduleStatus.Closed, ScheduleStatus.OnSale)]
    [InlineData(ScheduleStatus.Completed, ScheduleStatus.Draft)]
    [InlineData(ScheduleStatus.Completed, ScheduleStatus.OnSale)]
    [InlineData(ScheduleStatus.Completed, ScheduleStatus.Closed)]
    public void IsValidTransition_InvalidTransitions_ReturnsFalse(ScheduleStatus from, ScheduleStatus to)
    {
        ScheduleStatusTransitions.IsValidTransition(from, to).Should().BeFalse();
    }

    [Theory]
    [InlineData(ScheduleStatus.Draft, ScheduleStatus.Draft)]
    [InlineData(ScheduleStatus.OnSale, ScheduleStatus.OnSale)]
    [InlineData(ScheduleStatus.Closed, ScheduleStatus.Closed)]
    [InlineData(ScheduleStatus.Completed, ScheduleStatus.Completed)]
    public void IsValidTransition_SameStatus_ReturnsFalse(ScheduleStatus from, ScheduleStatus to)
    {
        ScheduleStatusTransitions.IsValidTransition(from, to).Should().BeFalse();
    }

    [Theory]
    [InlineData(ScheduleStatus.Draft, ScheduleStatus.Completed)]
    [InlineData(ScheduleStatus.Completed, ScheduleStatus.Draft)]
    public void ValidateTransition_InvalidTransition_ThrowsInvalidOperationException(ScheduleStatus from, ScheduleStatus to)
    {
        var act = () => ScheduleStatusTransitions.ValidateTransition(from, to);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{from}*{to}*");
    }

    [Theory]
    [InlineData(ScheduleStatus.Draft, ScheduleStatus.OnSale)]
    [InlineData(ScheduleStatus.OnSale, ScheduleStatus.Closed)]
    [InlineData(ScheduleStatus.Closed, ScheduleStatus.Completed)]
    public void ValidateTransition_ValidTransition_DoesNotThrow(ScheduleStatus from, ScheduleStatus to)
    {
        var act = () => ScheduleStatusTransitions.ValidateTransition(from, to);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(EventStatus.Draft)]
    [InlineData(EventStatus.Cancelled)]
    public void ValidateCanGoOnSale_InvalidEventStatus_ThrowsInvalidOperationException(EventStatus eventStatus)
    {
        var act = () => ScheduleStatusTransitions.ValidateCanGoOnSale(eventStatus);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{eventStatus}*");
    }

    [Theory]
    [InlineData(EventStatus.PendingReview)]
    [InlineData(EventStatus.Approved)]
    [InlineData(EventStatus.Published)]
    public void ValidateCanGoOnSale_ValidEventStatus_DoesNotThrow(EventStatus eventStatus)
    {
        var act = () => ScheduleStatusTransitions.ValidateCanGoOnSale(eventStatus);

        act.Should().NotThrow();
    }
}
