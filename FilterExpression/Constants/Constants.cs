namespace FilterExpression.Constants;
public static class Pattern
{
    public const string ValidCharacter = "^[A-Za-z0-9_.]+$";
    public const string GroupNot = @"\!\[group\d\]";
    public const string ConditionNot = @"\!\[condition\d\]";
    public const string Group = @"\[group\d\]";
    public const string Condition = @"\[condition\d\]";
}

public static class Operator
{
    public const string And = "&";
    public const string Or = "|";
    public const string Not = "!";
}