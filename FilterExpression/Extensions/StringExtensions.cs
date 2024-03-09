namespace FilterExpression.Extensions;

public static class StringExtensions
{
    public static List<string> GetStringList(this string commaString, string defaultValue = "")
    {
        if (commaString == null)
        {
            return null;
        }

        var results = new List<string>();
        var items = commaString.Split(',');

        foreach (var item in items)
        {
            results.Add((item ?? defaultValue));
        }

        return results;
    }
}
