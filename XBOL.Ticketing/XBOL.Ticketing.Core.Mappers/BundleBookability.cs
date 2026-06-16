using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Core.Mappers;

public static class BundleBookability
{
    public static bool IsBookable(Bundle bundle)
    {
        if (bundle.Status != EventStatus.Published
            || bundle.BundleType != BundleType.SeasonPass
            || string.IsNullOrWhiteSpace(bundle.ExternalKey))
        {
            return false;
        }

        var hasBookableSchedule = bundle.BundleEventSchedules.Any(link =>
            !string.IsNullOrWhiteSpace(link.EventSchedule?.ExternalEventKey));
        var hasForSaleSeat = bundle.BundleSections
            .SelectMany(section => section.BundleSeats)
            .Any(seat => seat.ForSale);

        return hasBookableSchedule && hasForSaleSeat;
    }
}
