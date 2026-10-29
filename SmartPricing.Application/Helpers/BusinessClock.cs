namespace SmartPricing.Application.Helpers;

/// <summary>
/// Timestamps are stored in UTC; business dates (price validity, "today", export dates)
/// follow the server's local time zone. Use these helpers instead of mixing DateTime.Today and UtcNow.
/// </summary>
public static class BusinessClock
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Local midnight of the current business day, expressed in UTC (for comparing stored timestamps).</summary>
    public static DateTime StartOfTodayUtc => DateTime.Today.ToUniversalTime();

    /// <summary>Business date of a stored UTC timestamp (EF returns them with Kind = Unspecified).</summary>
    public static DateOnly ToBusinessDate(DateTime utc) =>
        DateOnly.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime());
}
