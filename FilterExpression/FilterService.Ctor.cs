using FilterExpression.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        "contains", "eq", "gt", "ge", "in", "lt", "le", "ne", "startswith" 
    };
    private int _key = 0;

    public FilterService()
    {

    }
}
