using System.Net.WebSockets;

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

    public static List<int> GetIntList(this string commaString, int defaultValue = 0)
    {
        if (commaString == null)
        {
            return null;
        }
        var results = new List<int>();
        var items = commaString.Split(',');
        foreach (var item in items)
        {
            if (Int32.TryParse(item, out defaultValue))
            {
                results.Add(defaultValue);
            }
        }
        return results;
    }

    public static List<int?> GetNullableIntList(this string commaString)
    {
        if (commaString == null)
        {
            return null;
        }
        var results = new List<int?>();
        var items = commaString.Split(',');
        foreach (var item in items)
        {
            results.Add(item.ParseNullableInt());
        }
        return results;
    }
}
