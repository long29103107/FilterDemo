namespace FilterExpression.Models;
public abstract class BaseFilter
{
    public int Index { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
}
