using FilterExpression.Constants;
using FilterExpression.Models;

namespace FilterExpression;
public partial class FilterService
{
    private List<ConditionFilter> _conditionFilters = new List<ConditionFilter>();
    private List<GroupFilter> _groupFilters = new List<GroupFilter>();
    private List<ExpressionFilter> _fieldFilters = new List<ExpressionFilter>();
    private List<string> _validChar = new List<string>() { 
        "!", "", " ", "`", "(", ")", "|", "%", "&", ","
    }; 
    private List<string> _whiteListOperatior = new List<string>(){
        ComparisonOperator.Contains,
        ComparisonOperator.Equal,
        ComparisonOperator.GreaterThan,
        ComparisonOperator.GreaterThanAndEqual,
        ComparisonOperator.LessThan,
        ComparisonOperator.LessThanAndEqual,
        ComparisonOperator.NotEqual,
        ComparisonOperator.StartsWith,
        ComparisonOperator.In
    };
    private int _key = 0;

    public FilterService()
    {

    }
}
