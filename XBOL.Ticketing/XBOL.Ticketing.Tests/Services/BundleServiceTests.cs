using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Repositories.Media;
using XBOL.Ticketing.Services.Bundle;
using XBOL.Ticketing.Services.Media;

namespace XBOL.Ticketing.Tests.Services;

public class BundleServiceTests
{
    private readonly IBundleRepository _repository = Substitute.For<IBundleRepository>();
    private readonly IBaseSectionRepository _baseSectionRepository = Substitute.For<IBaseSectionRepository>();
    private readonly XBOLDbContext _dbContext = Substitute.For<XBOLDbContext>();
    private readonly MediaRepository _mediaRepository;
    private readonly MediaService _mediaService;
    private readonly IBundleLifecycleService _bundleLifecycleService = Substitute.For<IBundleLifecycleService>();
    private readonly BundleService _sut;

    public BundleServiceTests()
    {
        _mediaRepository = Substitute.For<MediaRepository>(_dbContext);
        _mediaService = Substitute.For<MediaService>(_mediaRepository);
        _sut = new BundleService(_repository, _baseSectionRepository, _mediaRepository, _mediaService, _bundleLifecycleService);
    }

    private static BundleCreateRequest ValidCreateRequest(BundlePricingType pricing = BundlePricingType.Single) => new()
    {
        VenueMapId = 1,
        OrganizerId = 1,
        Name = "Test Bundle",
        BundleType = BundleType.Basic,
        BundlePricingType = pricing
    };

    private void StubBundleSnapshot(params Bundle[] bundles)
    {
        _repository.Get(Arg.Any<Expression<Func<Bundle, bool>>>())
            .Returns(bundles.AsQueryable());
    }

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
    public async Task UpdateAsync_ValidStatusTransition_Succeeds()
    {
        var bundle = new Bundle { Id = 1, Status = EventStatus.Draft };
        _repository.GetByIdAsync(1).Returns(bundle);

        var result = await _sut.UpdateAsync(1,
            new BundleUpdateRequest { Status = EventStatus.PendingReview }, Guid.NewGuid());

        result!.Status.Should().Be(EventStatus.PendingReview);
    }

    [Fact]
    public async Task UpdateAsync_SeasonPassPublish_InvokesBundleLifecycle()
    {
        var userId = Guid.NewGuid();
        var bundle = new Bundle
        {
            Id = 1,
            Status = EventStatus.Approved,
            BundleType = BundleType.SeasonPass
        };
        _repository.GetByIdAsync(1).Returns(bundle);
        StubBundleSnapshot(new Bundle
        {
            Id = 1,
            Status = EventStatus.Published,
            BundleType = BundleType.SeasonPass,
            ExternalKey = "season-1"
        });

        await _sut.UpdateAsync(1,
            new BundleUpdateRequest { Status = EventStatus.Published }, userId);

        await _bundleLifecycleService.Received(1).PublishAsync(1, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_SeasonPassPublish_ReturnsReloadedPublishedBundle()
    {
        var userId = Guid.NewGuid();
        var bundle = new Bundle
        {
            Id = 1,
            Status = EventStatus.Approved,
            BundleType = BundleType.SeasonPass
        };
        var publishedBundle = new Bundle
        {
            Id = 1,
            Status = EventStatus.Published,
            BundleType = BundleType.SeasonPass,
            ExternalKey = "season-1"
        };
        _repository.GetByIdAsync(1).Returns(bundle);
        StubBundleSnapshot(publishedBundle);

        var result = await _sut.UpdateAsync(1,
            new BundleUpdateRequest { Status = EventStatus.Published }, userId);

        result!.Status.Should().Be(EventStatus.Published);
        result.ExternalKey.Should().Be("season-1");
    }

    [Fact]
    public async Task UpdateAsync_PublishWithOtherFields_AppliesFieldsBeforeLifecycle()
    {
        var userId = Guid.NewGuid();
        var bundle = new Bundle
        {
            Id = 1,
            Status = EventStatus.Approved,
            BundleType = BundleType.SeasonPass,
            Code = "OLD"
        };
        _repository.GetByIdAsync(1).Returns(bundle);
        StubBundleSnapshot(bundle);

        await _sut.UpdateAsync(1,
            new BundleUpdateRequest
            {
                Status = EventStatus.Published,
                Code = "NEW"
            },
            userId);

        bundle.Code.Should().Be("NEW");
        await _bundleLifecycleService.Received(1).PublishAsync(1, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_SeasonPassCancel_InvokesBundleLifecycle()
    {
        var userId = Guid.NewGuid();
        var bundle = new Bundle
        {
            Id = 1,
            Status = EventStatus.Published,
            BundleType = BundleType.SeasonPass,
            ExternalKey = "season-1"
        };
        _repository.GetByIdAsync(1).Returns(bundle);

        var result = await _sut.UpdateAsync(1,
            new BundleUpdateRequest { Status = EventStatus.Cancelled }, userId);

        result!.Status.Should().Be(EventStatus.Cancelled);
        result.ExternalKey.Should().BeNull();
        await _bundleLifecycleService.Received(1).CancelAsync(1, userId, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().UpdateAsync(bundle);
    }

    [Fact]
    public async Task UpdateAsync_PublishedSeasonPassNameChange_SyncsSeasonMetadataAfterLocalUpdate()
    {
        var userId = Guid.NewGuid();
        var bundle = new Bundle
        {
            Id = 1,
            Name = "Old Name",
            Status = EventStatus.Published,
            BundleType = BundleType.SeasonPass,
            ExternalKey = "season-1"
        };
        _repository.GetByIdAsync(1).Returns(bundle);

        var result = await _sut.UpdateAsync(1,
            new BundleUpdateRequest { Name = "New Name" }, userId);

        result!.Name.Should().Be("New Name");
        Received.InOrder(() =>
        {
            _repository.UpdateAsync(bundle);
            _bundleLifecycleService.SyncMetadataAsync(1, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task UpdateAsync_PublishedBasicNameChange_DoesNotSyncRemoteMetadata()
    {
        var bundle = new Bundle
        {
            Id = 1,
            Name = "Old Name",
            Status = EventStatus.Published,
            BundleType = BundleType.Basic
        };
        _repository.GetByIdAsync(1).Returns(bundle);

        await _sut.UpdateAsync(1,
            new BundleUpdateRequest { Name = "New Name" }, Guid.NewGuid());

        await _bundleLifecycleService.DidNotReceive().SyncMetadataAsync(
            Arg.Any<long>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_InvalidStatusTransition_ThrowsInvalidOperationException()
    {
        var bundle = new Bundle { Id = 1, Status = EventStatus.Draft };
        _repository.GetByIdAsync(1).Returns(bundle);

        var act = () => _sut.UpdateAsync(1,
            new BundleUpdateRequest { Status = EventStatus.Published }, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Draft*Published*");
    }

    [Fact]
    public async Task UpdateAsync_NullStatus_SkipsValidation()
    {
        var bundle = new Bundle { Id = 1, Name = "Test", Status = EventStatus.Draft };
        _repository.GetByIdAsync(1).Returns(bundle);

        var result = await _sut.UpdateAsync(1,
            new BundleUpdateRequest { Name = "Updated" }, Guid.NewGuid());

        result!.Status.Should().Be(EventStatus.Draft);
        result.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteAsync_NotFound_DoesNotAttemptDelete()
    {
        _repository.GetByIdAsync(999).Returns((Bundle?)null);

        await _sut.DeleteAsync(999);

        await _repository.DidNotReceive().HardDeleteAsync(Arg.Any<Bundle>());
    }
}
