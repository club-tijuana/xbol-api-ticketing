using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using XBOL.Ticketing.API.Controllers;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Repositories.Event;
using XBOL.Ticketing.Services.Event;

namespace XBOL.Ticketing.Tests.Controllers;

public class EventScheduleControllerTests
{
    [Fact]
    public async Task CreateScheduleAsync_ReturnsCreatedAtResolvableGetRoute()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var seedContext = new XBOLDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Events.Add(new Event
            {
                Id = 1,
                Name = "Opening Match",
                Status = EventStatus.Draft
            });
            await seedContext.SaveChangesAsync();
        }

        await using var context = new XBOLDbContext(options);
        var service = new EventScheduleService(
            new EventScheduleRepository(context),
            context,
            Substitute.For<IEventScheduleLifecycleService>());
        var controller = new EventScheduleController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.CreateScheduleAsync(CreateRequest());

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be("GetScheduleById");
    }

    private static EventScheduleRequest CreateRequest()
    {
        return new EventScheduleRequest
        {
            EventId = 1,
            OnSaleDate = new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero),
            OffSaleDate = new DateTimeOffset(2026, 6, 10, 18, 0, 0, TimeSpan.Zero),
            StartDateTime = new DateTimeOffset(2026, 6, 10, 19, 0, 0, TimeSpan.Zero),
            EndDateTime = new DateTimeOffset(2026, 6, 10, 22, 0, 0, TimeSpan.Zero),
            GameCategory = GameCategory.Regular,
            HoldExpirationInMinutes = 12
        };
    }
}
