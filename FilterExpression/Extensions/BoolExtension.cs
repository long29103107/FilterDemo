namespace FilterExpression.Extensions;
public static class BoolExtension
{
    public static bool? ParseNullableBool(this string value)
    {
        var result = new bool();

        if (string.IsNullOrEmpty(value) && !bool.TryParse(value, out result))
        {
            return null;
        }

        return result;
    }
}
