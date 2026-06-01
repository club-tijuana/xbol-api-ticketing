using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Security.Claims;
using XBOL.Ticketing.API.Controllers;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Data.Repositories.Media;
using XBOL.Ticketing.Services.Bundle;
using XBOL.Ticketing.Services.Media;

namespace XBOL.Ticketing.Tests.Controllers;

public sealed class BundleControllerTests
{
    [Fact]
    public async Task CreateBundle_ReturnsCreatedLocation()
    {
        var repository = Substitute.For<IBundleRepository>();
        repository.GetCategoriesByIdsAsync(Arg.Any<IReadOnlyCollection<long>>())
            .Returns([]);
        repository.InsertAsync(Arg.Do<Bundle>(bundle => bundle.Id = 44))
            .Returns(Task.CompletedTask);
        repository.CommitAsync().Returns(Task.CompletedTask);

        var eventScheduleRepository = Substitute.For<IEventScheduleRepository>();
        eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(101)
            .Returns(new EventSchedule
            {
                Id = 101,
                Status = ScheduleStatus.OnSale,
                ExternalEventKey = "event-101",
                StartDateTime = DateTimeOffset.UtcNow.AddDays(7),
                Event = new Event
                {
                    VenueMapId = 11,
                    Status = EventStatus.Published
                }
            });
        var baseSectionRepository = Substitute.For<IBaseSectionRepository>();
        baseSectionRepository.Get(includedProperties: Arg.Any<string[]>())
            .Returns(Enumerable.Empty<BaseSection>().AsQueryable());

        var service = new BundleService(
            repository,
            baseSectionRepository,
            Substitute.For<IBundleEventScheduleRepository>(),
            eventScheduleRepository,
            null!,
            null!,
            Substitute.For<IBundleLifecycleService>());
        var controller = new BundleController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim("sub", Guid.NewGuid().ToString())],
                        "Test"))
                }
            }
        };

        var response = await controller.CreateBundle(new BundleCreateRequest
        {
            VenueMapId = 11,
            OrganizerId = 12,
            Name = "Bundle",
            BundleType = BundleType.Basic,
            BundlePricingType = BundlePricingType.Composite,
            EventScheduleIds = [101]
        });

        var created = response.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(BundleController.GetBundleById));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(44);
        created.Value.Should().BeOfType<BundleDTO>().Which.Id.Should().Be(44);
    }
}
