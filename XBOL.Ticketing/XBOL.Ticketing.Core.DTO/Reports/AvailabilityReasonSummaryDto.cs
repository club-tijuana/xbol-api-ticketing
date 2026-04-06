namespace XBOL.Ticketing.Core.DTO.Reports;

/// <summary>
/// Summary by availability reason (available, booked, reservedByToken, not_for_sale, custom statuses).
/// Keys: "available", "booked", "reservedByToken", "not_for_sale", and any custom status.
/// </summary>
public class AvailabilityReasonSummaryDto : SectionSummaryDto;
