namespace MV.DomainLayer.Helpers;

public static class DateTimeHelper
{
    private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }

    public static DateTime VietnamNow()
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);

    public static DateTime VietnamToday()
        => VietnamNow().Date;

    public static DateOnly VietnamTodayDateOnly()
        => DateOnly.FromDateTime(VietnamNow());
}
