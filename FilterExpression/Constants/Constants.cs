namespace FilterExpression.Constants;
public static class Pattern
{
    public const string ValidCharacter = "^[A-Za-z0-9_.]+$";
    public const string GroupNot = @"\!\[group\d\]";
    public const string ConditionNot = @"\!\[condition\d\]";
    public const string Group = @"\[group\d\]";
    public const string Condition = @"\[condition\d\]";
}

public static class RelationalOperator
{
    public const string And = "&";
    public const string Or = "|";
    public const string Not = "!";
}

public static class ComparisonOperator
{
    public const string Equal = "eq";
    public const string NotEqual = "ne";
    public const string GreaterThan = "gt";
    public const string GreaterThanAndEqual = "ge";
    public const string LessThan = "lt";
    public const string LessThanAndEqual = "le";
    public const string Contains = "contains";
    public const string StartsWith = "startsWith";
    public const string In = "in";
}