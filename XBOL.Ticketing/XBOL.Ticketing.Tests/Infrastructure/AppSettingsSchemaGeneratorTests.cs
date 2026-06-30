using System.Text.Json.Nodes;
using FluentAssertions;
using XBOL.Ticketing.API.Schema;

namespace XBOL.Ticketing.Tests.Infrastructure;

public sealed class AppSettingsSchemaGeneratorTests
{
    [Fact]
    public void Generate_includes_cors_settings_for_secret_catalog()
    {
        var schema = AppSettingsSchemaGenerator.Generate();

        var cors = schema["properties"]?["Cors"] as JsonObject;
        cors.Should().NotBeNull();
        cors!["properties"]?["PolicyName"].Should().NotBeNull();
        cors["properties"]?["AcceptedOrigins"].Should().NotBeNull();
    }

    [Fact]
    public void Generate_includes_evo_settings_with_required_fields()
    {
        var schema = AppSettingsSchemaGenerator.Generate();

        var evo = schema["properties"]?["EvoSettings"] as JsonObject;
        evo.Should().NotBeNull();
        evo!["properties"]?["APIPassword"].Should().NotBeNull();
        evo["properties"]?["MerchantId"].Should().NotBeNull();
        evo["properties"]?["Version"].Should().NotBeNull();

        var required = evo["required"]?.AsArray();
        required.Should().ContainEquivalentOf("APIPassword")
            .And.ContainEquivalentOf("MerchantId")
            .And.ContainEquivalentOf("Version");
    }

    [Fact]
    public void Generate_includes_background_job_diagnostics_secret_with_required_field()
    {
        var schema = AppSettingsSchemaGenerator.Generate();

        var diagnostics = schema["properties"]?["BackgroundJobDiagnostics"] as JsonObject;
        diagnostics.Should().NotBeNull();
        diagnostics!["properties"]?["SharedSecret"].Should().NotBeNull();
        diagnostics["properties"]?["SharedSecret"]?["description"]?.GetValue<string>()
            .Should().Be("Shared key required in the X-XBOL-Diagnostics-Key header for internal background-job diagnostic probes");

        var topLevelRequired = schema["required"]?.AsArray();
        topLevelRequired.Should().ContainEquivalentOf("BackgroundJobDiagnostics");

        var required = diagnostics["required"]?.AsArray();
        required.Should().ContainEquivalentOf("SharedSecret");
    }

    [Fact]
    public void Generate_marks_background_jobs_section_required_for_secret_catalog()
    {
        var schema = AppSettingsSchemaGenerator.Generate();

        var backgroundJobs = schema["properties"]?["BackgroundJobs"] as JsonObject;
        backgroundJobs.Should().NotBeNull();
        backgroundJobs!["properties"]?["ConnectionString"].Should().NotBeNull();

        var topLevelRequired = schema["required"]?.AsArray();
        topLevelRequired.Should().ContainEquivalentOf("BackgroundJobs");

        var required = backgroundJobs["required"]?.AsArray();
        required.Should().ContainEquivalentOf("ConnectionString");
    }
}
