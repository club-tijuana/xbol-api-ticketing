using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SeatsioDotNet.Reports.Events;
using XBOL.Ticketing.Core.DTO.Reports;
using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.API.Controllers;

[Route("api/reports/{key}")]
[ApiController]
[Tags("Reports")]
public class ReportsController(SeatsIoService seatsIoService) : ControllerBase
{
    /// <summary>
    /// Retrieves a summary report of seat availability, grouped by section.
    /// </summary>
    [HttpGet("summary/by-section")]
    [EndpointName("GetSectionSummaryAsync")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Dictionary<string, SectionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, SectionSummaryDto>>> GetSectionSummaryAsync(
        [FromRoute, Required] string key)
    {
        var report = await seatsIoService.GetSummaryBySectionAsync(key);
        return Ok(MapReport<SectionSummaryDto>(report));
    }

    /// <summary>
    /// Retrieves a summary report of seat availability, grouped by zone.
    /// </summary>
    [HttpGet("summary/by-zone")]
    [EndpointName("GetZoneSummaryAsync")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Dictionary<string, ZoneSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, ZoneSummaryDto>>> GetZoneSummaryAsync(
        [FromRoute, Required] string key)
    {
        var report = await seatsIoService.GetSummaryByZoneAsync(key);
        return Ok(MapReport<ZoneSummaryDto>(report));
    }

    /// <summary>
    /// Retrieves a summary report of seat availability, grouped by availability (available vs not_available).
    /// </summary>
    [HttpGet("summary/by-availability")]
    [EndpointName("GetAvailabilitySummaryAsync")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Dictionary<string, AvailabilitySummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, AvailabilitySummaryDto>>> GetAvailabilitySummaryAsync(
        [FromRoute, Required] string key)
    {
        var report = await seatsIoService.GetSummaryByAvailabilityAsync(key);
        return Ok(MapReport<AvailabilitySummaryDto>(report));
    }

    /// <summary>
    /// Retrieves a summary report of seat availability, grouped by availability reason
    /// (available, booked, reservedByToken, not_for_sale, and any custom statuses).
    /// </summary>
    [HttpGet("summary/by-availability-reason")]
    [EndpointName("GetAvailabilityReasonSummaryAsync")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Dictionary<string, AvailabilityReasonSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, AvailabilityReasonSummaryDto>>> GetAvailabilityReasonSummaryAsync(
        [FromRoute, Required] string key)
    {
        var report = await seatsIoService.GetSummaryByAvailabilityReasonAsync(key);
        return Ok(MapReport<AvailabilityReasonSummaryDto>(report));
    }

    private static Dictionary<string, T> MapReport<T>(Dictionary<string, EventReportSummaryItem> report)
        where T : SectionSummaryDto, new()
    {
        var result = new Dictionary<string, T>();

        foreach (var (key, value) in report)
        {
            result.Add(key, new T
            {
                Count = value.Count,
                ByStatus = value.byStatus,
                ByAvailability = value.byAvailability,
                ByCategoryLabel = value.byCategoryLabel,
                ByCategoryKey = value.byCategoryKey,
                ByChannel = value.byChannel
            });
        }

        return result;
    }
}
