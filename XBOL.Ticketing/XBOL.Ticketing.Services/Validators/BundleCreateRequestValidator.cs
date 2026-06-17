using FluentValidation;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;

namespace XBOL.Ticketing.Services.Validators;

public class BundleCreateRequestValidator : AbstractValidator<BundleCreateRequest>
{
    public BundleCreateRequestValidator()
    {
        RuleFor(x => x.VenueMapId).GreaterThan(0);
        RuleFor(x => x.OrganizerId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CategoryIds)
            .NotEmpty()
            .WithMessage("At least one category must be selected.");
        RuleFor(x => x.BundleType)
            .IsInEnum()
            .Must(type => type is BundleType.Basic or BundleType.SeasonPass)
            .WithMessage("Bundle type must be Basic or Season Pass.");
        RuleFor(x => x.BundlePricingType).IsInEnum();
        RuleFor(x => x.BundlePricingType)
            .Equal(BundlePricingType.Composite)
            .When(x => x.BundleType == BundleType.Basic)
            .WithMessage("Basic bundles must use Composite pricing.");
        RuleFor(x => x.BundlePricingType)
            .Equal(BundlePricingType.Single)
            .When(x => x.BundleType == BundleType.SeasonPass)
            .WithMessage("SeasonPass bundles must use Single pricing.");
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue && x.StartDate.HasValue);

        RuleFor(x => x.PreSaleDate)
            .GreaterThan(x => x.PublishedDate)
            .When(x => x.PreSaleDate.HasValue && x.PublishedDate.HasValue);

        RuleFor(x => x.OnSaleDate)
            .NotNull()
            .WithMessage("On sale date is required.");

        RuleFor(x => x.OffSaleDate)
            .NotNull()
            .WithMessage("Off sale date is required.");

        RuleFor(x => x.OnSaleDate)
            .GreaterThan(x => x.PreSaleDate)
            .When(x => x.OnSaleDate.HasValue && x.PreSaleDate.HasValue);

        RuleFor(x => x.OffSaleDate)
            .GreaterThan(x => x.OnSaleDate)
            .When(x => x.OffSaleDate.HasValue && x.OnSaleDate.HasValue);

        RuleFor(x => x.RenewalEndDate)
            .GreaterThan(x => x.RenewalStartDate)
            .When(x => x.RenewalEndDate.HasValue && x.RenewalStartDate.HasValue);

        RuleFor(x => x.RenewalStartDate)
            .NotNull()
            .When(x => x.PreviousBundleId.HasValue)
            .WithMessage("Renewal start date is required for renewal bundles.");

        RuleFor(x => x.RenewalEndDate)
            .NotNull()
            .When(x => x.PreviousBundleId.HasValue)
            .WithMessage("Renewal end date is required for renewal bundles.");
    }
}
