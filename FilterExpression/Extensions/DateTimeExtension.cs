namespace FilterExpression.Extensions;
public static class DateTimeExtension
{
    public static DateTime? ParseNullableDateTime(this string value)
    {
        DateTime result = new DateTime();

        if (string.IsNullOrEmpty(value) || !DateTime.TryParse(value, out result))
            return null;

        if (value.ToCharArray()[value.Length - 1] == 'Z')
        {
            result = TimeZoneInfo.ConvertTimeToUtc(result);
        }

        return result;
    }
}
