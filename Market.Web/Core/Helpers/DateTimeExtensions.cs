using Market.Web.Core.Exceptions;

namespace Market.Web.Core.Helpers;

public static class DateTimeExtensions
{
    // IANA ID — works on Linux (Docker) and Windows without extra configuration
    private static readonly TimeZoneInfo PolandTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

    /// <summary>
    /// Converts a UTC DateTime to Polish local time (CET/CEST) for display purposes only.
    /// Never use the returned value for storage or comparison — it is local time.
    /// </summary>
    public static DateTime ToPolandTime(this DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Local)
            dateTime = dateTime.ToUniversalTime();
        else if (dateTime.Kind == DateTimeKind.Unspecified)
            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(dateTime, PolandTz);
    }

    /// <summary>
    /// Converts a Poland local time DateTime (as submitted by a datetime-local browser input)
    /// back to UTC for storage. Kind is expected to be Unspecified — treated as Poland local.
    /// Throws AuctionException if the time falls in the DST gap (e.g. 02:30 on spring-forward day).
    /// </summary>
    public static DateTime ToUtcFromPolandTime(this DateTime localDateTime)
    {
        if (localDateTime.Kind == DateTimeKind.Utc)
            return localDateTime;

        try
        {
            var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(unspecified, PolandTz);
        }
        catch (ArgumentException)
        {
            // The entered time does not exist — it falls in the DST spring-forward gap
            throw new AuctionException(
                "Podana data i godzina nie istnieje — w tym czasie zegarki są przestawiane do przodu (zmiana czasu). Wybierz inną godzinę.",
                nameof(localDateTime));
        }
    }
}
