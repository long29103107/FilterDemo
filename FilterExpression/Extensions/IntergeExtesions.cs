namespace FilterExpression.Extensions;
public static class IntergeExtesions
{
    public static int? ParseNullableInt(this string value)
    {
        var result = new int();

        if (string.IsNullOrEmpty(value) && !int.TryParse(value, out result))
            return null;

        return result;
    }
}
