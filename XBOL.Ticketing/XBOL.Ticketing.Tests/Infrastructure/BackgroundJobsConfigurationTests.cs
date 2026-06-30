using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XBOL.Ticketing.API.Extensions;
using CommonBackgroundJobsOptions = Odasoft.XBOL.Commons.Options.BackgroundJobsOptions;

namespace XBOL.Ticketing.Tests.Infrastructure;

public sealed class BackgroundJobsConfigurationTests
{
    [Fact]
    public void ConfigureOptions_BindsCommonBackgroundJobsOptions()
    {
        const string connectionString = "Host=db;Database=xbol;Username=xbol;Password=secret";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundJobs:ConnectionString"] = connectionString
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        services.ConfigureOptions(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptions<CommonBackgroundJobsOptions>>()
            .Value;
        options.ConnectionString.Should().Be(connectionString);
    }

    [Fact]
    public void TicketingCoreCommons_DoesNotDefineSeparateBackgroundJobsOptions()
    {
        var duplicateType = typeof(XBOL.Ticketing.Core.Commons.Options.SeatsIoOptions)
            .Assembly
            .GetType("XBOL.Ticketing.Core.Commons.Options.BackgroundJobsOptions");

        duplicateType.Should().BeNull(
            "Ticketing must use the shared Odasoft.XBOL.Commons background-job options contract");
    }
}
