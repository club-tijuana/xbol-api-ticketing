using FluentValidation.TestHelper;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Services.Validators;

namespace XBOL.Ticketing.Tests.Validators;

public class BundleCreateRequestValidatorTests
{
    private readonly BundleCreateRequestValidator _sut = new();

    private static BundleCreateRequest ValidRequest() => new()
    {
        VenueMapId = 1,
        OrganizerId = 1,
        Name = "Test Bundle",
        BundleType = BundleType.Basic,
        BundlePricingType = BundlePricingType.Single
    };

    [Fact]
    public async Task ValidRequest_Passes()
    {
        var result = await _sut.TestValidateAsync(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task VenueMapId_Zero_Fails()
    {
        var request = new BundleCreateRequest
        {
            VenueMapId = 0, OrganizerId = 1, Name = "Test",
            BundleType = BundleType.Basic, BundlePricingType = BundlePricingType.Single
        };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.VenueMapId);
    }

    [Fact]
    public async Task OrganizerId_Zero_Fails()
    {
        var request = new BundleCreateRequest
        {
            VenueMapId = 1, OrganizerId = 0, Name = "Test",
            BundleType = BundleType.Basic, BundlePricingType = BundlePricingType.Single
        };
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.OrganizerId);
    }

    [Fact]
    public async Task Name_Empty_Fails()
    {
        var request = ValidRequest();
        request.Name = "";
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Name_Exceeds200_Fails()
    {
        var request = ValidRequest();
        request.Name = new string('x', 201);
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task BundleType_InvalidEnum_Fails(int value)
    {
        var request = ValidRequest();
        request.BundleType = (BundleType)value;
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.BundleType);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task BundlePricingType_InvalidEnum_Fails(int value)
    {
        var request = ValidRequest();
        request.BundlePricingType = (BundlePricingType)value;
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.BundlePricingType);
    }

    [Fact]
    public async Task EndDate_BeforeStartDate_Fails()
    {
        var request = ValidRequest();
        request.StartDate = DateTimeOffset.UtcNow;
        request.EndDate = DateTimeOffset.UtcNow.AddDays(-1);
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public async Task PreSaleDate_BeforePublishedDate_Fails()
    {
        var request = ValidRequest();
        request.PublishedDate = DateTimeOffset.UtcNow;
        request.PreSaleDate = DateTimeOffset.UtcNow.AddDays(-1);
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.PreSaleDate);
    }

    [Fact]
    public async Task OnSaleDate_BeforePreSaleDate_Fails()
    {
        var request = ValidRequest();
        request.PreSaleDate = DateTimeOffset.UtcNow;
        request.OnSaleDate = DateTimeOffset.UtcNow.AddDays(-1);
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.OnSaleDate);
    }

    [Fact]
    public async Task OffSaleDate_BeforeOnSaleDate_Fails()
    {
        var request = ValidRequest();
        request.OnSaleDate = DateTimeOffset.UtcNow;
        request.OffSaleDate = DateTimeOffset.UtcNow.AddDays(-1);
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.OffSaleDate);
    }

    [Fact]
    public async Task RenewalEndDate_BeforeRenewalStartDate_Fails()
    {
        var request = ValidRequest();
        request.RenewalStartDate = DateTimeOffset.UtcNow;
        request.RenewalEndDate = DateTimeOffset.UtcNow.AddDays(-1);
        var result = await _sut.TestValidateAsync(request);
        result.ShouldHaveValidationErrorFor(x => x.RenewalEndDate);
    }
}
