using FluentValidation.TestHelper;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Services.Validators;

namespace XBOL.Ticketing.Tests.Validators;

public class BundleUpdateRequestValidatorTests
{
    private readonly BundleUpdateRequestValidator _sut = new();

    private static BundleUpdateRequest ValidRequest() => new()
    {
        Name = "Updated Bundle",
        Status = EventStatus.Draft,
        BundleType = BundleType.Basic,
        BundlePricingType = BundlePricingType.Composite
    };

    [Fact]
    public async Task AllFieldsNull_Passes()
    {
        var result = await _sut.TestValidateAsync(new BundleUpdateRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidFields_Passes()
    {
        var result = await _sut.TestValidateAsync(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Name_Empty_WhenProvided_Fails()
    {
        var request = new BundleUpdateRequest { Name = "" };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Name_Exceeds200_WhenProvided_Fails()
    {
        var request = new BundleUpdateRequest { Name = new string('x', 201) };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task BundleType_InvalidEnum_WhenProvided_Fails(int value)
    {
        var request = new BundleUpdateRequest { BundleType = (BundleType)value };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.BundleType);
    }

    [Fact]
    public async Task BundleType_Group_WhenProvided_Fails()
    {
        var request = new BundleUpdateRequest { BundleType = BundleType.Group };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.BundleType)
            .WithErrorMessage("Bundle type must be Basic or Season Pass.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task BundlePricingType_InvalidEnum_WhenProvided_Fails(int value)
    {
        var request = new BundleUpdateRequest { BundlePricingType = (BundlePricingType)value };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.BundlePricingType);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task Status_InvalidEnum_WhenProvided_Fails(int value)
    {
        var request = new BundleUpdateRequest { Status = (EventStatus)value };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public async Task EndDate_BeforeStartDate_WhenBothProvided_Fails()
    {
        var request = new BundleUpdateRequest
        {
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(-1)
        };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public async Task EndDate_OnlyStartDateProvided_Passes()
    {
        var request = new BundleUpdateRequest { StartDate = DateTimeOffset.UtcNow };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public async Task DatePairs_WhenOnlyOneSideProvided_Pass()
    {
        var now = DateTimeOffset.UtcNow;
        var requests = new[]
        {
            new BundleUpdateRequest { EndDate = now },
            new BundleUpdateRequest { PublishedDate = now },
            new BundleUpdateRequest { PreSaleDate = now },
            new BundleUpdateRequest { OnSaleDate = now },
            new BundleUpdateRequest { OffSaleDate = now },
            new BundleUpdateRequest { RenewalStartDate = now },
            new BundleUpdateRequest { RenewalEndDate = now }
        };

        foreach (var request in requests)
        {
            var result = await _sut.TestValidateAsync(request);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    [Fact]
    public async Task OffSaleDate_BeforeOnSaleDate_WhenBothProvided_Fails()
    {
        var request = new BundleUpdateRequest
        {
            OnSaleDate = DateTimeOffset.UtcNow,
            OffSaleDate = DateTimeOffset.UtcNow.AddDays(-1)
        };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.OffSaleDate);
    }

    [Fact]
    public async Task FirstSaleSeasonPass_WithRenewalWindow_WhenProvided_Fails()
    {
        var now = DateTimeOffset.UtcNow;
        var request = new BundleUpdateRequest
        {
            BundleType = BundleType.SeasonPass,
            PreviousBundleId = null,
            RenewalStartDate = now.AddDays(2),
            RenewalEndDate = now.AddDays(5)
        };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.RenewalStartDate);
        result.ShouldHaveValidationErrorFor(x => x.RenewalEndDate);
    }

    [Fact]
    public async Task RenewalBundle_OnSaleBeforeRenewalEnd_WhenProvided_Fails()
    {
        var now = DateTimeOffset.UtcNow;
        var request = new BundleUpdateRequest
        {
            BundleType = BundleType.SeasonPass,
            PreviousBundleId = 10,
            RenewalStartDate = now.AddDays(2),
            RenewalEndDate = now.AddDays(5),
            OnSaleDate = now.AddDays(1)
        };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.OnSaleDate);
    }

    [Fact]
    public async Task RenewalBundle_PreSaleBeforeRenewalEnd_WhenProvided_Fails()
    {
        var now = DateTimeOffset.UtcNow;
        var request = new BundleUpdateRequest
        {
            BundleType = BundleType.SeasonPass,
            PreviousBundleId = 10,
            RenewalStartDate = now.AddDays(2),
            RenewalEndDate = now.AddDays(5),
            PreSaleDate = now.AddDays(4),
            OnSaleDate = now.AddDays(6)
        };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.PreSaleDate);
    }
}
