using FluentAssertions;
using NSubstitute;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Services.Bundle;

namespace XBOL.Ticketing.Tests.Services;

public class BundlePassServiceTests
{
    private readonly IBundlePassRepository _repository = Substitute.For<IBundlePassRepository>();
    private readonly BundlePassService _sut;

    public BundlePassServiceTests()
    {
        _sut = new BundlePassService(_repository);
    }

    private static BundlePassCreateRequest ValidCreateRequest() => new()
    {
        BundleId = 1,
        BundlePassType = BundlePassType.Full,
        Price = 100m
    };

    [Fact]
    public async Task CreateAsync_TrackingCode_Is16CharUppercaseHex()
    {
        var result = await _sut.CreateAsync(ValidCreateRequest(), Guid.NewGuid());

        result.TrackingCode.Should().MatchRegex("^[A-F0-9]{16}$");
    }

    [Fact]
    public async Task CreateAsync_PrivateToken_IsValidGuid()
    {
        var result = await _sut.CreateAsync(ValidCreateRequest(), Guid.NewGuid());

        Guid.TryParse(result.PrivateToken, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_EachPassGetsDistinctTrackingCodeAndToken()
    {
        var r1 = await _sut.CreateAsync(ValidCreateRequest(), Guid.NewGuid());
        var r2 = await _sut.CreateAsync(ValidCreateRequest(), Guid.NewGuid());

        r1.TrackingCode.Should().NotBe(r2.TrackingCode);
        r1.PrivateToken.Should().NotBe(r2.PrivateToken);
    }

    [Fact]
    public async Task UpdateAsync_SuspendRequiresReason()
    {
        var pass = new BundlePass { Id = 1, Status = BundlePassStatus.Active };
        _repository.GetByIdAsync(1).Returns(pass);

        var result = await _sut.UpdateAsync(1, new BundlePassUpdateRequest
        {
            Status = BundlePassStatus.Suspended,
            SuspendedReason = BundlePassSuspendedReason.FraudSuspicion,
            SuspendedOtherReason = "Suspicious activity detected"
        }, Guid.NewGuid());

        result.Status.Should().Be(BundlePassStatus.Suspended);
        result.SuspendedReason.Should().Be(BundlePassSuspendedReason.FraudSuspicion);
        result.SuspendedOtherReason.Should().Be("Suspicious activity detected");
    }

    [Fact]
    public async Task UpdateAsync_NullFieldsLeftUntouched()
    {
        var pass = new BundlePass
        {
            Id = 1, Price = 100m, BundleSeatId = 42, IsDigital = true
        };
        _repository.GetByIdAsync(1).Returns(pass);

        var result = await _sut.UpdateAsync(1, new BundlePassUpdateRequest
        {
            Status = BundlePassStatus.Suspended,
            SuspendedReason = BundlePassSuspendedReason.AdminDecision
        }, Guid.NewGuid());

        result.Price.Should().Be(100m);
        result.BundleSeatId.Should().Be(42);
        result.IsDigital.Should().BeTrue();
    }
}
