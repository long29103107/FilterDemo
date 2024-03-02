using System.Linq.Expressions;

namespace FilterExpression.Models;
public sealed class GroupFilter : BaseFilter
{
    public Expression Expression { get; set; } = null;
}