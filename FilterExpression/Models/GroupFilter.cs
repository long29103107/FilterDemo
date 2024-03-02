using System.Linq.Expressions;

namespace FilterExpression.Models;
public class GroupFilter : BaseFilter
{
    public Expression Expression { get; set; } = null;
}