using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SeatsioDotNet.Charts;
using System.Reflection;
using System.Runtime.CompilerServices;
using XBOL.Ticketing.API.Controllers;
using XBOL.Ticketing.Core.DTO.Responses;

namespace XBOL.Ticketing.Tests.Controllers;

public sealed class ChartsControllerTests
{
    [Fact]
    public void ToChartResponse_TreatsMissingSeatsIoValidationAsEmpty()
    {
        var method = typeof(ChartsController).GetMethod(
            "ToChartResponse",
            BindingFlags.NonPublic | BindingFlags.Static);
        var chart = (Chart)RuntimeHelpers.GetUninitializedObject(typeof(Chart));

        var response = method!.Invoke(null, [chart]).Should()
            .BeOfType<ChartResponse>()
            .Subject;

        response.Key.Should().BeEmpty();
        response.Name.Should().BeEmpty();
        response.Status.Should().BeEmpty();
        response.PublishedVersionThumbnailUrl.Should().BeEmpty();
        response.DraftVersionThumbnailUrl.Should().BeEmpty();
        response.VenueType.Should().BeEmpty();
        response.Validation.Errors.Should().BeEmpty();
        response.Validation.Warnings.Should().BeEmpty();
    }
}
