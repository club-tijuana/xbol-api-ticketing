using FluentAssertions;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Tests.Models;

public class EventSeasonLegacyTests
{
    [Fact]
    public void EventModel_DoesNotExposeLegacySeasonNavigation()
    {
        typeof(Event).GetProperty("SeasonId").Should().BeNull();
        typeof(Event).GetProperty("Season").Should().BeNull();
    }

    [Fact]
    public void BundleContracts_DoNotExposeLegacySeasonId()
    {
        typeof(BundleDTO).GetProperty("SeasonId").Should().BeNull();
        typeof(BundleCreateRequest).GetProperty("SeasonId").Should().BeNull();
    }
}
