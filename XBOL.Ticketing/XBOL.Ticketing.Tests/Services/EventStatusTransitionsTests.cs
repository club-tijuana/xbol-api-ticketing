using FluentAssertions;
using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Tests.Services;

public class EventStatusTransitionsTests
{
    [Theory]
    [InlineData(EventStatus.Draft, EventStatus.PendingReview)]
    [InlineData(EventStatus.PendingReview, EventStatus.Approved)]
    [InlineData(EventStatus.Approved, EventStatus.Published)]
    public void IsValidTransition_ValidForwardTransitions_ReturnsTrue(EventStatus from, EventStatus to)
    {
        EventStatusTransitions.IsValidTransition(from, to).Should().BeTrue();
    }

    [Theory]
    [InlineData(EventStatus.PendingReview, EventStatus.Draft)]
    public void IsValidTransition_ValidBackwardTransitions_ReturnsTrue(EventStatus from, EventStatus to)
    {
        EventStatusTransitions.IsValidTransition(from, to).Should().BeTrue();
    }

    [Theory]
    [InlineData(EventStatus.PendingReview, EventStatus.ChangesRequested)]
    [InlineData(EventStatus.ChangesRequested, EventStatus.Draft)]
    [InlineData(EventStatus.ChangesRequested, EventStatus.PendingReview)]
    public void IsValidTransition_ChangesRequestedReviewTransitions_ReturnsTrue(EventStatus from, EventStatus to)
    {
        EventStatusTransitions.IsValidTransition(from, to).Should().BeTrue();
    }

    [Theory]
    [InlineData(EventStatus.Draft, EventStatus.Cancelled)]
    [InlineData(EventStatus.PendingReview, EventStatus.Cancelled)]
    [InlineData(EventStatus.Approved, EventStatus.Cancelled)]
    [InlineData(EventStatus.Published, EventStatus.Cancelled)]
    public void IsValidTransition_ValidTerminalTransitions_ReturnsTrue(EventStatus from, EventStatus to)
    {
        EventStatusTransitions.IsValidTransition(from, to).Should().BeTrue();
    }

    [Theory]
    [InlineData(EventStatus.Published, EventStatus.Draft)]
    [InlineData(EventStatus.Published, EventStatus.PendingReview)]
    [InlineData(EventStatus.Published, EventStatus.Approved)]
    [InlineData(EventStatus.Cancelled, EventStatus.Draft)]
    [InlineData(EventStatus.Cancelled, EventStatus.PendingReview)]
    [InlineData(EventStatus.Cancelled, EventStatus.Approved)]
    [InlineData(EventStatus.Cancelled, EventStatus.Published)]
    [InlineData(EventStatus.Approved, EventStatus.Draft)]
    [InlineData(EventStatus.Draft, EventStatus.Published)]
    [InlineData(EventStatus.Draft, EventStatus.Approved)]
    public void IsValidTransition_InvalidTransitions_ReturnsFalse(EventStatus from, EventStatus to)
    {
        EventStatusTransitions.IsValidTransition(from, to).Should().BeFalse();
    }

    [Theory]
    [InlineData(EventStatus.Draft, EventStatus.Draft)]
    [InlineData(EventStatus.PendingReview, EventStatus.PendingReview)]
    [InlineData(EventStatus.Approved, EventStatus.Approved)]
    [InlineData(EventStatus.ChangesRequested, EventStatus.ChangesRequested)]
    [InlineData(EventStatus.Published, EventStatus.Published)]
    [InlineData(EventStatus.Cancelled, EventStatus.Cancelled)]
    public void IsValidTransition_SameStatus_ReturnsFalse(EventStatus from, EventStatus to)
    {
        EventStatusTransitions.IsValidTransition(from, to).Should().BeFalse();
    }

    [Theory]
    [InlineData(EventStatus.Published, EventStatus.Draft)]
    [InlineData(EventStatus.Cancelled, EventStatus.Draft)]
    public void ValidateTransition_InvalidTransition_ThrowsInvalidOperationException(EventStatus from, EventStatus to)
    {
        var act = () => EventStatusTransitions.ValidateTransition(from, to);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{from}*{to}*");
    }

    [Theory]
    [InlineData(EventStatus.Draft, EventStatus.PendingReview)]
    [InlineData(EventStatus.Approved, EventStatus.Published)]
    public void ValidateTransition_ValidTransition_DoesNotThrow(EventStatus from, EventStatus to)
    {
        var act = () => EventStatusTransitions.ValidateTransition(from, to);

        act.Should().NotThrow();
    }
}
