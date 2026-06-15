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
}
