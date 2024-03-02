using FilterExpression.Extensions;
using System.Linq.Expressions;

namespace FilterExpression.Models;
public sealed class ExpressionFilter
{
    public ParameterExpression ParaExp { get; set; }
    public string PropertyName { get; set; }
    public string StrType { get; set; }
    public string StrValue { get; set; }
    public Expression Expression { get; set; } = null;
    public string Key { get; set; }
    public Type Type
    {
        get
        {
            return StrType.ToType();
        }
    }
}
