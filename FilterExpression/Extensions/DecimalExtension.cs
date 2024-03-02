namespace FilterExpression.Extensions;
public static class DecimalExtension
{
    public static decimal? ParseNullableDecimal(this string value)
    {
        var result = new decimal();

        if (string.IsNullOrEmpty(value) && !decimal.TryParse(value, out result))
        {
            return null;
        }

        return result;
    }
}
