using FluentAssertions;
using XBOL.Ticketing.API.Extensions;
using XBOL.Ticketing.API.Options;

namespace XBOL.Ticketing.Tests.Infrastructure;

public sealed class CorsConfigurationTests
{
    [Fact]
    public void AcceptedOrigins_removes_blank_origins_and_trims_configured_values()
    {
        var options = new CorsOptions
        {
            AcceptedOrigins =
            [
                " https://web.pwrticket.mx ",
                "",
                "   ",
                "https://qa-web.pwrticket.mx"
            ]
        };

        CorsConfiguration.AcceptedOrigins(options).Should().Equal(
            "https://web.pwrticket.mx",
            "https://qa-web.pwrticket.mx");
    }

    [Fact]
    public void PolicyName_defaults_to_xbol_policy_when_unset()
    {
        var options = new CorsOptions { PolicyName = " " };

        CorsConfiguration.PolicyName(options).Should().Be("XBOLPolicy");
    }
}
