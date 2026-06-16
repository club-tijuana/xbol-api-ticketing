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
    private static readonly DateTimeOffset FutureStart = DateTimeOffset.UtcNow.AddDays(30);

    private readonly IBundleRepository _repository = Substitute.For<IBundleRepository>();
    private readonly IBaseSectionRepository _baseSectionRepository = Substitute.For<IBaseSectionRepository>();
    private readonly IBundleEventScheduleRepository _bundleEventScheduleRepository = Substitute.For<IBundleEventScheduleRepository>();
    private readonly IEventScheduleRepository _eventScheduleRepository = Substitute.For<IEventScheduleRepository>();
    private readonly XBOLDbContext _dbContext = Substitute.For<XBOLDbContext>();
    private readonly MediaRepository _mediaRepository;
    private readonly MediaService _mediaService;
    private readonly IBundleLifecycleService _bundleLifecycleService = Substitute.For<IBundleLifecycleService>();
    private readonly BundleService _sut;

    public BundleServiceTests()
    {
        _mediaRepository = Substitute.For<MediaRepository>(_dbContext);
        _mediaService = Substitute.For<MediaService>(_mediaRepository);
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(101)
            .Returns(Schedule(101));
        _bundleEventScheduleRepository.GetByEventScheduleIdAsync(Arg.Any<long>())
            .Returns([]);
        _sut = new BundleService(
            _repository,
            _baseSectionRepository,
            _bundleEventScheduleRepository,
            _eventScheduleRepository,
            _mediaRepository,
            _mediaService,
            _bundleLifecycleService);
    }

    private static BundleCreateRequest ValidCreateRequest(BundlePricingType pricing = BundlePricingType.Single) => new()
    {
        VenueMapId = 1,
        OrganizerId = 1,
        Name = "Test Bundle",
        BundleType = BundleType.SeasonPass,
        BundlePricingType = pricing,
        EventScheduleIds = [101]
    };

    private static EventSchedule Schedule(
        long id,
        long venueMapId = 1,
        ScheduleStatus status = ScheduleStatus.Draft,
        EventStatus eventStatus = EventStatus.PendingReview,
        string? externalEventKey = null,
        DateTimeOffset? startDateTime = null,
        DateTimeOffset? deletedAt = null) => new()
        {
            Id = id,
            Status = status,
            ExternalEventKey = externalEventKey,
            StartDateTime = startDateTime ?? FutureStart,
            DeletedAt = deletedAt,
            Event = new Event
            {
                VenueMapId = venueMapId,
                Status = eventStatus
            }
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
    public async Task CreateAsync_BasicCompositePricing_CreatesBundleSectionsAndSeats()
    {
        var sections = new List<BaseSection>
        {
            new()
            {
                Id = 10,
                Name = "B",
                BaseZone = new() { Id = 1, VenueMapId = 1 },
                BaseRows =
                [
                    new()
                    {
                        Id = 100,
                        RowLabel = "1",
                        BaseSeats =
                        [
                            new() { Id = 1000, SeatNumber = "1" },
                            new() { Id = 1001, SeatNumber = "2" }
                        ]
                    }
                ]
            },
            new()
            {
                Id = 20,
                Name = "C",
                BaseZone = new() { Id = 2, VenueMapId = 2 },
                BaseRows =
                [
                    new()
                    {
                        Id = 200,
                        RowLabel = "1",
                        BaseSeats =
                        [
                            new() { Id = 2000, SeatNumber = "1" }
                        ]
                    }
                ]
            }
        }.AsQueryable();
        _baseSectionRepository.Get(includedProperties: Arg.Any<string[]>())
            .Returns(sections);
        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.Basic;
        Bundle? insertedBundle = null;
        _repository.InsertAsync(Arg.Do<Bundle>(bundle => insertedBundle = bundle))
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, Guid.NewGuid());

        await _repository.Received(1).InsertAsync(Arg.Any<Bundle>());
        insertedBundle.Should().NotBeNull();
        var bundleSection = insertedBundle!.BundleSections.Should().ContainSingle().Subject;
        bundleSection.BaseSectionId.Should().Be(10);
        bundleSection.DisplayName.Should().Be("B");
        bundleSection.TotalSeats.Should().Be(2);
        bundleSection.AvailableSeats.Should().Be(2);
        bundleSection.BundleSeats.Select(seat => seat.BaseSeatId).Should().Equal([1000, 1001]);
        bundleSection.BundleSeats.Select(seat => seat.ExternalSeatObjectKey).Should().Equal(["B-1-1", "B-1-2"]);
        bundleSection.BundleSeats.Should().OnlyContain(seat => seat.ForSale);
    }

    [Fact]
    public async Task CreateAsync_BasicBundleRejectsSinglePricing()
    {
        var request = ValidCreateRequest(BundlePricingType.Single);
        request.BundleType = BundleType.Basic;

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Basic*Composite*");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task CreateAsync_SeasonPassRejectsCompositePricing()
    {
        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.SeasonPass;

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SeasonPass*Single*");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task CreateAsync_GroupBundleRejectsUnsupportedBundleType()
    {
        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.Group;

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Bundle type must be Basic or Season Pass.");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task CreateAsync_DefaultsStatusToDraft()
    {
        var result = await _sut.CreateAsync(ValidCreateRequest(), Guid.NewGuid());
        result.Status.Should().Be(EventStatus.Draft);
    }

    [Fact]
    public async Task CreateAsync_PreservesEventFieldsAndCategories()
    {
        var categories = new List<EventCategory>
        {
            new() { Id = 7, Name = "soccer", DisplayName = "Soccer", IsActive = true }
        };
        _repository.GetCategoriesByIdsAsync(Arg.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new long[] { 7 })))
            .Returns(categories);

        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.Basic;
        request.AgeRestriction = AgeRestriction.AllAges;
        request.SecurityPolicies = "Clear bag";
        request.AdditionalComments = "Arrive early";
        request.CategoryIds = [7];

        var result = await _sut.CreateAsync(request, Guid.NewGuid());

        await _repository.Received(1).InsertAsync(Arg.Is<Bundle>(b =>
            b.AgeRestriction == AgeRestriction.AllAges &&
            b.SecurityPolicies == "Clear bag" &&
            b.AdditionalComments == "Arrive early" &&
            b.Categories.Count == 1 &&
            b.Categories[0].Id == 7));
        result.OrganizerId.Should().Be(request.OrganizerId);
        result.AgeRestriction.Should().Be(AgeRestriction.AllAges);
        result.SecurityPolicies.Should().Be("Clear bag");
        result.AdditionalComments.Should().Be("Arrive early");
        result.Categories.Should().ContainSingle().Which.Id.Should().Be(7);
    }

    [Fact]
    public async Task CreateAsync_AttachesSelectedEventSchedules()
    {
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(101)
            .Returns(Schedule(101));
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(102)
            .Returns(Schedule(102));
        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.Basic;
        request.EventScheduleIds = [101, 102];

        var result = await _sut.CreateAsync(request, Guid.NewGuid());

        await _repository.Received(1).InsertAsync(Arg.Is<Bundle>(bundle =>
            bundle.BundleEventSchedules.Select(link => link.EventScheduleId).SequenceEqual(new long[] { 101, 102 }) &&
            bundle.BundleEventSchedules.Select(link => link.SortOrder).SequenceEqual(new int?[] { 0, 1 })));
        result.Schedules.Select(schedule => schedule.Id).Should().Equal(101, 102);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateEventScheduleIds()
    {
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(101)
            .Returns(Schedule(101));
        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.Basic;
        request.EventScheduleIds = [101, 101];

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already selected*");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingEventScheduleIds()
    {
        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.Basic;
        request.EventScheduleIds = [];

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*At least one event schedule*");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task CreateAsync_SeasonPassRejectsSchedulesWithExternalEventKey()
    {
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(101)
            .Returns(Schedule(101, externalEventKey: "event-key"));
        var request = ValidCreateRequest(BundlePricingType.Single);
        request.BundleType = BundleType.SeasonPass;
        request.EventScheduleIds = [101];

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has ExternalEventKey*");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task CreateAsync_RejectsScheduleFromBundleEvent()
    {
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(101)
            .Returns(new EventSchedule
            {
                Id = 101,
                Status = ScheduleStatus.Draft,
                StartDateTime = FutureStart,
                Event = new Bundle { Id = 44, VenueMapId = 1 }
            });
        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.Basic;
        request.EventScheduleIds = [101];

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must belong to an Event, not a Bundle*");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task CreateAsync_RejectsScheduleFromDifferentVenueMap()
    {
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(101)
            .Returns(Schedule(101, venueMapId: 2));
        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.Basic;
        request.VenueMapId = 1;
        request.EventScheduleIds = [101];

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*different venue map*");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task CreateAsync_RejectsPastEventSchedules()
    {
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(101)
            .Returns(Schedule(101, startDateTime: DateTimeOffset.UtcNow.AddDays(-1)));
        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.Basic;

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*past*");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task CreateAsync_RejectsDeletedEventSchedules()
    {
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(101)
            .Returns(Schedule(101, deletedAt: DateTimeOffset.UtcNow));
        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.Basic;

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*deleted*");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Theory]
    [InlineData(ScheduleStatus.Closed)]
    [InlineData(ScheduleStatus.Completed)]
    public async Task CreateAsync_RejectsInactiveEventSchedules(ScheduleStatus status)
    {
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(101)
            .Returns(Schedule(101, status: status));
        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.Basic;

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{status}*");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Theory]
    [InlineData(EventStatus.Draft)]
    [InlineData(EventStatus.ChangesRequested)]
    [InlineData(EventStatus.Cancelled)]
    public async Task CreateAsync_RejectsEventSchedulesWhoseParentEventIsNotSelectable(EventStatus status)
    {
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(101)
            .Returns(Schedule(101, eventStatus: status));
        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.Basic;

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{status}*");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task CreateAsync_BasicBundleRejectsOnSaleScheduleWithoutExternalEventKey()
    {
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(101)
            .Returns(Schedule(101, status: ScheduleStatus.OnSale));
        var request = ValidCreateRequest(BundlePricingType.Composite);
        request.BundleType = BundleType.Basic;

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*OnSale*ExternalEventKey*");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task CreateAsync_SeasonPassRejectsNonDraftSchedules()
    {
        _eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(101)
            .Returns(Schedule(101, status: ScheduleStatus.OnSale));
        var request = ValidCreateRequest(BundlePricingType.Single);
        request.BundleType = BundleType.SeasonPass;

        var act = async () => await _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SeasonPass*Draft*");
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task UpdateAsync_OnlyOverwritesProvidedFields()
    {
        var bundle = new Bundle
        {
            Id = 1,
            Name = "Original",
            Subtitle = "Keep Me",
            Status = EventStatus.Draft,
            BundleType = BundleType.Basic,
            BundlePricingType = BundlePricingType.Composite
        };
        _repository.GetByIdAsync(1).Returns(bundle);

        var result = await _sut.UpdateAsync(1, new BundleUpdateRequest { Name = "Changed" }, Guid.NewGuid());

        result!.Name.Should().Be("Changed");
        result.Subtitle.Should().Be("Keep Me");
        result.Status.Should().Be(EventStatus.Draft);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEventMetadataAndCategories()
    {
        var category = new EventCategory
        {
            Id = 7,
            Name = "soccer",
            DisplayName = "Soccer",
            IsActive = true
        };
        var bundle = new Bundle
        {
            Id = 1,
            Name = "Original",
            Status = EventStatus.Draft,
            BundleType = BundleType.Basic,
            BundlePricingType = BundlePricingType.Composite,
            Categories =
            [
                new EventCategory
                {
                    Id = 3,
                    Name = "old",
                    DisplayName = "Old",
                    IsActive = true
                }
            ]
        };
        _repository.GetByIdAsync(1).Returns(bundle);
        _repository.GetCategoriesByIdsAsync(Arg.Any<IReadOnlyCollection<long>>())
            .Returns([category]);

        var result = await _sut.UpdateAsync(1, new BundleUpdateRequest
        {
            AgeRestriction = AgeRestriction.AllAges,
            SecurityPolicies = "Clear bag",
            AdditionalComments = "Arrive early",
            CategoryIds = [7]
        }, Guid.NewGuid());

        result!.AgeRestriction.Should().Be(AgeRestriction.AllAges);
        result.SecurityPolicies.Should().Be("Clear bag");
        result.AdditionalComments.Should().Be("Arrive early");
        result.Categories.Should().ContainSingle(item =>
            item.Id == 7 &&
            item.Name == "soccer" &&
            item.DisplayName == "Soccer");
        await _repository.Received(1).GetCategoriesByIdsAsync(
            Arg.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new long[] { 7 })));
    }

    [Fact]
    public async Task UpdateAsync_RejectsBundleTypeChanges()
    {
        var bundle = new Bundle
        {
            Id = 1,
            Status = EventStatus.Draft,
            BundleType = BundleType.SeasonPass,
            BundlePricingType = BundlePricingType.Single
        };
        _repository.GetByIdAsync(1).Returns(bundle);

        var act = async () => await _sut.UpdateAsync(1,
            new BundleUpdateRequest { BundleType = BundleType.Basic },
            Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BundleType*cannot be changed*");
        bundle.BundleType.Should().Be(BundleType.SeasonPass);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task UpdateAsync_RejectsBundlePricingTypeChanges()
    {
        var bundle = new Bundle
        {
            Id = 1,
            Status = EventStatus.Draft,
            BundleType = BundleType.SeasonPass,
            BundlePricingType = BundlePricingType.Single
        };
        _repository.GetByIdAsync(1).Returns(bundle);

        var act = async () => await _sut.UpdateAsync(1,
            new BundleUpdateRequest { BundlePricingType = BundlePricingType.Composite },
            Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BundlePricingType*cannot be changed*");
        bundle.BundlePricingType.Should().Be(BundlePricingType.Single);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Bundle>());
    }

    [Fact]
    public async Task UpdateAsync_AllowsSameBundleClassificationValues()
    {
        var bundle = new Bundle
        {
            Id = 1,
            Name = "Original",
            Status = EventStatus.Draft,
            BundleType = BundleType.SeasonPass,
            BundlePricingType = BundlePricingType.Single
        };
        _repository.GetByIdAsync(1).Returns(bundle);

        var result = await _sut.UpdateAsync(1,
            new BundleUpdateRequest
            {
                Name = "Changed",
                BundleType = BundleType.SeasonPass,
                BundlePricingType = BundlePricingType.Single
            },
            Guid.NewGuid());

        result!.Name.Should().Be("Changed");
        result.BundleType.Should().Be(BundleType.SeasonPass);
        result.BundlePricingType.Should().Be(BundlePricingType.Single);
        await _repository.Received(1).UpdateAsync(bundle);
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
