using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;

namespace XBOL.Ticketing.Tests.Models;

public sealed class OrderSchemaCompatibilityTests
{
    [Fact]
    public void Order_model_does_not_map_hosted_checkout_context_columns_missing_from_schema()
    {
        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new XBOLDbContext(options);

        var orderEntity = context.Model.FindEntityType(typeof(Order));

        orderEntity.Should().NotBeNull();
        orderEntity!.FindProperty("EventScheduleId").Should().BeNull();
        orderEntity.FindProperty("HoldToken").Should().BeNull();
    }
}
