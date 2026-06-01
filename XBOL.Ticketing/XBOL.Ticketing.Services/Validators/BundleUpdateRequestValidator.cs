using FluentValidation;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;

namespace XBOL.Ticketing.Services.Validators;

public class BundleUpdateRequestValidator : AbstractValidator<BundleUpdateRequest>
{
    public BundleUpdateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Name is not null);

        RuleFor(x => x.BundleType)
            .IsInEnum()
            .Must(type => type is BundleType.Basic or BundleType.SeasonPass)
            .WithMessage("Bundle type must be Basic or Season Pass.")
            .When(x => x.BundleType is not null);

        RuleFor(x => x.BundlePricingType)
            .IsInEnum()
            .When(x => x.BundlePricingType is not null);

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status is not null);

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
