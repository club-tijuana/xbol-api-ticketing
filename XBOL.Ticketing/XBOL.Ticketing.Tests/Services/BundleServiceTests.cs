using FluentAssertions;
using NSubstitute;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Services.Bundle;

namespace XBOL.Ticketing.Tests.Services;

public class BundleServiceTests
{
    private readonly IBundleRepository _repository = Substitute.For<IBundleRepository>();
    private readonly IBaseSectionRepository _baseSectionRepository = Substitute.For<IBaseSectionRepository>();
    private readonly BundleService _sut;

    public BundleServiceTests()
    {
        _sut = new BundleService(_repository, _baseSectionRepository);
    }

    private static BundleCreateRequest ValidCreateRequest(BundlePricingType pricing = BundlePricingType.Single) => new()
    {
        VenueMapId = 1,
        OrganizerId = 1,
        Name = "Test Bundle",
        BundleType = BundleType.Basic,
        BundlePricingType = pricing
    };

    [Fact]
    public async Task CreateAsync_SinglePricing_AutoCreatesOneBundleSectionPerBaseSection()
    {
        var sections = new List<BaseSection>
        {
            new() { Id = 10, BaseZone = new() { Id = 1, VenueMapId = 1 } },
            new() { Id = 20, BaseZone = new() { Id = 2, VenueMapId = 1 } },
            new() { Id = 30, BaseZone = new() { Id = 3, VenueMapId = 1 } }
        }.AsQueryable();

        _baseSectionRepository.Get(includedProperties: Arg.Any<string[]>())
            .Returns(sections);

        await _sut.CreateAsync(ValidCreateRequest(BundlePricingType.Single), Guid.NewGuid());

        await _repository.Received(1).InsertAsync(Arg.Is<Bundle>(b =>
            b.BundleSections.Count == 3));
    }

    [Fact]
    public async Task CreateAsync_SinglePricing_FiltersSectionsByVenueMapId()
    {
        var sections = new List<BaseSection>
        {
            new() { Id = 10, BaseZone = new() { Id = 1, VenueMapId = 1 } },
            new() { Id = 20, BaseZone = new() { Id = 2, VenueMapId = 2 } }
        }.AsQueryable();

        _baseSectionRepository.Get(includedProperties: Arg.Any<string[]>())
            .Returns(sections);

        var request = ValidCreateRequest(BundlePricingType.Single);
        request.VenueMapId = 1;

        await _sut.CreateAsync(request, Guid.NewGuid());

        await _repository.Received(1).InsertAsync(Arg.Is<Bundle>(b =>
            b.BundleSections.Count == 1 &&
            b.BundleSections[0].BaseSectionId == 10));
    }

    [Fact]
    public async Task CreateAsync_CompositePricing_NoBundleSections()
    {
        await _sut.CreateAsync(ValidCreateRequest(BundlePricingType.Composite), Guid.NewGuid());

        await _repository.Received(1).InsertAsync(Arg.Is<Bundle>(b =>
            b.BundleSections == null || b.BundleSections.Count == 0));
    }

    [Fact]
    public async Task CreateAsync_DefaultsStatusToDraft()
    {
        var result = await _sut.CreateAsync(ValidCreateRequest(), Guid.NewGuid());
        result.Status.Should().Be(EventStatus.Draft);
    }

    [Fact]
    public async Task UpdateAsync_OnlyOverwritesProvidedFields()
    {
        var bundle = new Bundle
        {
            Id = 1, Name = "Original", Subtitle = "Keep Me", Status = EventStatus.Draft
        };
        _repository.GetByIdAsync(1).Returns(bundle);

        var result = await _sut.UpdateAsync(1, new BundleUpdateRequest { Name = "Changed" }, Guid.NewGuid());

        result!.Name.Should().Be("Changed");
        result.Subtitle.Should().Be("Keep Me");
        result.Status.Should().Be(EventStatus.Draft);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_DoesNotAttemptDelete()
    {
        _repository.GetByIdAsync(999).Returns((Bundle?)null);

        await _sut.DeleteAsync(999);

        await _repository.DidNotReceive().HardDeleteAsync(Arg.Any<Bundle>());
    }
}
