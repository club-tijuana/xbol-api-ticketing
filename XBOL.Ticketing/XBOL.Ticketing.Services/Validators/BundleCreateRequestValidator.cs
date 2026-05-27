using FluentValidation;
using XBOL.Ticketing.Core.DTO.Requests;

namespace XBOL.Ticketing.Services.Validators;

public class BundleCreateRequestValidator : AbstractValidator<BundleCreateRequest>
{
    public BundleCreateRequestValidator()
    {
        RuleFor(x => x.VenueMapId).GreaterThan(0);
        RuleFor(x => x.OrganizerId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BundleType).IsInEnum();
        RuleFor(x => x.BundlePricingType).IsInEnum();

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue && x.StartDate.HasValue);

        RuleFor(x => x.PreSaleDate)
            .GreaterThan(x => x.PublishedDate)
            .When(x => x.PreSaleDate.HasValue && x.PublishedDate.HasValue);

        RuleFor(x => x.OnSaleDate)
            .GreaterThan(x => x.PreSaleDate)
            .When(x => x.OnSaleDate.HasValue && x.PreSaleDate.HasValue);

        RuleFor(x => x.OffSaleDate)
            .GreaterThan(x => x.OnSaleDate)
            .When(x => x.OffSaleDate.HasValue && x.OnSaleDate.HasValue);

        RuleFor(x => x.RenewalEndDate)
            .GreaterThan(x => x.RenewalStartDate)
            .When(x => x.RenewalEndDate.HasValue && x.RenewalStartDate.HasValue);
    }
}
