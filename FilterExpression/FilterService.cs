using FilterExpression.Directive.Implement;
using FilterExpression.Directive;
using FilterExpression.Models;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using FilterExpression.Extensions;

namespace FilterExpression;

public partial class FilterService
{

    public Expression<Func<T, bool>> Filter<T>(string fe)
    {
        var tempFe = fe;
        Expression<Func<T, bool>> result = null;
        Type typeOfGeneric = typeof(T);
        ParameterExpression pe = Expression.Parameter(typeOfGeneric, "x");

        if (string.IsNullOrEmpty(tempFe))
        {
            return result;
        }

        try
        {
            //1. Validate Expression 
            _ValidateFilterExpression(tempFe);

            //2. Get Condition Expression
            _ConditionFilterExpression(ref tempFe);

            //3. Get Group Expression
            _GroupFilterExpression(ref tempFe);

            //4. Parse Filter Expression
            _ParseFieldFilter(ref pe, typeOfGeneric);

            //5. Add Condition To Group
            _AddExpressionToGroup();

            Expression body = _groupFilters.OrderByDescending(x => x.Index)
               .FirstOrDefault()?.Expression ?? null;

            return body == null ? null : Expression.Lambda<Func<T, bool>>(body, pe);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            _key = 0;
            _key = 0;
            _groupFilters.Clear();
            _conditionFilters.Clear();
        }

        return result;
    }

    #region ==================== 1. Validate Expression ====================
    private void _ValidateFilterExpression(string fe)
    {
        List<string> feString = fe.Select(x => x.ToString()).ToList();

        var invalidChar = feString.Where(x => !Regex.IsMatch(x.Trim(), "^[A-Za-z0-9_.]+$") 
                && !_validChar.Contains(x.Trim()))
            .Distinct()
            .ToList();

        if (invalidChar.Any())
        {
            throw new Exception("Invalid filter request");
        }

        var openBracketCount = feString.Count(x => x.Equals("("));
        var closeBracketCount = feString.Count(x => x.Equals(")"));

        if (openBracketCount != closeBracketCount)
        {
            throw new Exception($"Request has {openBracketCount} open bracket and {closeBracketCount} close bracket");
        }

        if (!(new List<string>() { "(", "!" }).Contains(feString.FirstOrDefault() ?? string.Empty))
        {
            throw new Exception($"First character of fe must be `(` or `!`");
        }

        if (!(feString.LastOrDefault() ?? string.Empty).Equals(")"))
        {
            throw new Exception($"Last character of fe must be `)`");
        }
    }
    #endregion ==================== 1. Validate Expression ====================

    #region ==================== 2. Get Condition Expression ====================
    private void _ConditionFilterExpression(ref string fe)
    {
        var indexOfSharp = 0;

        for (var i = 0; i < fe.Length; i++)
        {
            var character = fe[i].ToString();

            if (character.Equals("("))
            {
                indexOfSharp = i;
            }
            else if (character.Equals("!") || character.Equals("|") || character.Equals("&"))
            {
                indexOfSharp++;
            }
            else if (character.Equals(")"))
            {
                int length = (i - indexOfSharp) + 1;
                string tempFeFilter = fe.Substring(indexOfSharp, length);

                if (string.IsNullOrEmpty(tempFeFilter)) continue;

                if (!_conditionFilters.Any(x => x.Value.Equals(tempFeFilter)))
                {
                    var key = $"[condition{_key++}]";
                    _conditionFilters.Add(new ConditionFilter
                    {
                        Index = _key,
                        Key = key,
                        Value = tempFeFilter
                    });
                }

                indexOfSharp += length + 1;
            }
        }

        foreach (var filter in _conditionFilters)
        {
            fe = fe.Replace(filter.Value, filter.Key);
        }
    }
    #endregion ==================== 2. Get Condition Expression ====================

    #region ==================== 3. Get Group Expression ====================
    private void _GroupFilterExpression(ref string fe)
    {
        var indexOfSharp = 0;
        var pattern = @"\!\[group\d\]";

        var charNeedToGroup = new List<string>() { "!", "|", "&" };

        if (string.IsNullOrEmpty(fe)) return;

        if (fe.Any(c => c.ToString().Equals("(")))
        {
            for (var i = 0; i < fe.Length; i++)
            {
                if (indexOfSharp > i) continue;

                var character = fe[i].ToString();

                if (character.Equals("("))
                {
                    indexOfSharp = i;
                }
                else if (character.Equals(")"))
                {
                    int length = (i - indexOfSharp) + 1;
                    string tempFeFilter = fe.Substring(indexOfSharp, length);

                    if (string.IsNullOrEmpty(tempFeFilter)) continue;

                    if (!_groupFilters.Any(x => x.Value.Equals(tempFeFilter)))
                    {
                        var key = $"[group{_key}]";
                        _groupFilters.Add(new GroupFilter
                        {
                            Index = _key,
                            Key = key,
                            Value = tempFeFilter
                        });

                        _key++;
                    }

                    indexOfSharp += length + 1;
                }
                else
                {
                    continue;
                }
            }

            foreach (var groupFilter in _groupFilters)
            {
                if (fe.Contains(groupFilter.Value))
                {
                    fe = fe.Replace(groupFilter.Value, groupFilter.Key);
                }
            }
        }
        else if (Regex.Matches(fe, pattern).Any())
        {
            var groups = Regex.Matches(fe, pattern).Select(x => x as Match).ToList();

            foreach (var group in groups)
            {
                var key = $"[group{_key}]";
                _groupFilters.Add(new GroupFilter
                {
                    Index = _key,
                    Key = key,
                    Value = group.Value
                });

                _key++;
            }

            foreach (var groupFilter in _groupFilters)
            {
                if (fe.Contains(groupFilter.Value))
                {
                    fe = fe.Replace(groupFilter.Value, groupFilter.Key);
                }
            }
        }
        else if (fe.Any(c => c.ToString().Equals("&")) || fe.Any(c => c.ToString().Equals("|")))
        {
            var key = $"[group{_key}]";
            _groupFilters.Add(new GroupFilter
            {
                Index = _key,
                Key = key,
                Value = fe
            });

            fe = string.Empty;
        }

        _GroupFilterExpression(ref fe);
    }
    #endregion ==================== 3. Get Group Expression ====================

    #region ==================== 4. Parse Filter Expression ====================
    public void _ParseFieldFilter(ref ParameterExpression pe, Type type)
    {
        foreach (var item in _conditionFilters)
        {
            if (string.IsNullOrEmpty(item.Value)) continue;

            List<string> splitStr = item.Value.Replace("(", "").Replace(")", "").Split(' ').ToList();

            if (splitStr.Count != 3)
            {
                throw new Exception($"Request `{item.Value}` invalid");
            }

            string firstValue = splitStr[0] ?? string.Empty;
            string secondValue = splitStr[1] ?? string.Empty;
            string thirdValue = splitStr[2] ?? string.Empty;

            //TODO: valid name field in white list
            PropertyInfo? prop = type.GetProperty(firstValue);

            if (prop == null)
            {
                throw new Exception("Property name is not exist");
            }

            var valueTypeString = prop?.PropertyType.ToTypeNameOrAlias().ToLower();

            //TODO: valid operator in white list
            if (string.IsNullOrEmpty(secondValue))
            {
                throw new Exception("Operator must have value");
            }

            if (!_whiteListOperatior.Contains(secondValue))
            {
                throw new Exception($"Operator must be one of the keywords `{string.Join(", ", _whiteListOperatior)}` ");
            }

            //TODO: get value value type
            if (!thirdValue.StartsWith("`") || !thirdValue.EndsWith("`"))
            {
                throw new Exception("Value of filter must in ``");
            }

            thirdValue = thirdValue.Replace("`", "").Trim();

            Expression? body = null;

            MemberExpression me = Expression.Property(pe, firstValue);

            var typeProperty = _ParseStringToType(valueTypeString);

            ConstantExpression constant = Expression.Constant(_ParseValue(thirdValue, typeProperty), typeProperty);

            var expressionName = _GetGenerateExpression(me, constant, secondValue ?? string.Empty);

            if (body == null || string.IsNullOrEmpty(secondValue))
            {
                body = expressionName;
            }
            else
            {
                if (secondValue == Constants.Operator.And)
                    body = Expression.And(body, expressionName);
                else if (secondValue == Constants.Operator.Or)
                    body = Expression.Or(body, expressionName);
            }

            var fieldFilter = new ExpressionFilter()
            {
                ParaExp = pe,
                PropertyName = firstValue,
                StrValue = thirdValue,
                StrType = valueTypeString,
                Expression = body,
                Key = item.Key
            };

            _fieldFilters.Add(fieldFilter);
        }
    }

    public static Expression _GetGenerateExpression(MemberExpression me, ConstantExpression constant, string strOperator)
    {
        IFilterDirective filterDirective = null;

        if (strOperator == "contains")
            filterDirective = new ContainsDirective();
        else if (strOperator == "eq")
            filterDirective = new EqualDirective();
        else if (strOperator == "gt")
            filterDirective = new GreaterThanDirective();
        else if (strOperator == "ge")
            filterDirective = new GreaterThanOrEqualDirective();
        else if (strOperator == "in")
            filterDirective = new InArrayDirective();
        else if (strOperator == "lt")
            filterDirective = new LessThanDirective();
        else if (strOperator == "le")
            filterDirective = new LessThanOrEqualDirective();
        else if (strOperator == "ne")
            filterDirective = new NotEqualDirective();
        else if (strOperator == "startswith")
            filterDirective = new StartsWithDirective();

        return filterDirective.GenerateExpression(ref me, constant);
    }

    private static Type _ParseStringToType(string strType)
    {
        if (strType == "string")
        {
            return typeof(string);
        }
        else if (strType == "int")
        {
            return typeof(int);
        }
        else if (strType == "decimal")
        {
            return typeof(decimal);
        }
        else if (strType == "datetime")
        {
            return typeof(DateTime);
        }
        else if (!string.IsNullOrEmpty(strType))
        {
            throw new Exception($"Value Type `{strType}` is not supported yet.");
        }

        return null;
    }

    private static object _ParseValue(string value, Type type)
    {
        var v = value.Trim();

        if (type == typeof(string)) return v;

        if (type == typeof(DateTime)) return DateTime.Parse(v);
        //if (type == typeof(DateTime?)) return v.ParseNullableDateTime();

        if (type == typeof(int)) return int.Parse(v);
        //if (type == typeof(int?)) return v.ParseNullableInt();

        if (type == typeof(decimal)) return decimal.Parse(v);
        //if (type == typeof(decimal?)) return v.ParseNullableDecimal();

        //if (type == typeof(bool)) return filterSetting.IsNot ? !bool.Parse(v) : bool.Parse(v);
        //if (type == typeof(bool?)) return filterSetting.IsNot ? !v.ParseNullableBool() : v.ParseNullableBool();

        throw new Exception($"Convert value `{value}` to type `{type}` is not supported yet.");
    }

    #endregion ==================== 4. Parse Filter Expression ====================

    #region ==================== 5. Add Condition To Group ====================
    private void _AddExpressionToGroup()
    {
        var groupPattern = @"\[group\d\]";
        var conditionPattern = @"\[condition\d\]";

        foreach (var group in _groupFilters.OrderBy(x => x.Key).ToList())
        {
            var groupList = Regex.Matches(group.Value, groupPattern);
            var conditionList = Regex.Matches(group.Value, conditionPattern);

            if (conditionList.Any() && groupList.Any())
            {
                group.Expression = _GetExpressionOfConditionAndGroup(group);
            }
            else if (conditionList.Any())
            {
                group.Expression = _GetExpressionOfCondition(group);
            }
            else if (groupList.Any())
            {
                group.Expression = _GetExpressionOfGroup(group);
            }
        }
    }

    private Expression _GetExpressionOfConditionAndGroup(GroupFilter group)
    {
        Expression result = null;

        var groupPattern = @"\[group\d\]";
        var conditionPattern = @"\[condition\d\]";

        var valueString = group.Value;

        var mapGroupFilters = Regex.Matches(group.Value, groupPattern)
            .Select(x => x as Match)
            .Select(x => new ExpressionMapFilter()
            {
                Key = x.Value,
                StartIndex = x.Index,
                EndIndex = (x.Index + x.Value.Length - 1),
            })
            .ToList();

        var mapConditionFilters = Regex.Matches(group.Value, conditionPattern)
            .Select(x => x as Match)
            .Select(x => new ExpressionMapFilter()
            {
                Key = x.Value,
                StartIndex = x.Index,
                EndIndex = (x.Index + x.Value.Length - 1),
            })
            .ToList();

        if (mapGroupFilters.IsNullOrEmpty() || mapConditionFilters.IsNullOrEmpty())
        {
            return null;
        }

        //Get group first
        foreach (var item in mapGroupFilters)
        {
            var groupFilter = _groupFilters.FirstOrDefault(x => x.Key == item.Key);

            var tempExp = _GetExpressionOfGroup(groupFilter);

            var index = valueString.IndexOf(item.Key);

            if (index - 1 >= 0)
            {
                if (valueString[index - 1].ToString().Equals("!"))
                {
                    tempExp = Expression.Not(tempExp);
                }
                else if (valueString[index - 1].ToString().Equals("|"))
                {
                    tempExp = result == null ? tempExp : Expression.Or(result, tempExp);
                }
                else if (valueString[index - 1].ToString().Equals("&"))
                {
                    tempExp = result == null ? tempExp : Expression.And(result, tempExp);
                }
            }

            result = tempExp;
        }

        //Get condition 
        foreach (var item in mapConditionFilters)
        {
            var conditionFilter = _fieldFilters.FirstOrDefault(x => x.Key == item.Key);

            var tempExp = conditionFilter.Expression;

            var index = valueString.IndexOf(item.Key);

            if (index - 1 >= 0)
            {
                if (valueString[index - 1].ToString().Equals("!"))
                {
                    tempExp = Expression.Not(tempExp);
                }
                else if (valueString[index - 1].ToString().Equals("|"))
                {
                    tempExp = result == null ? tempExp : Expression.Or(result, tempExp);
                }
                else if (valueString[index - 1].ToString().Equals("&"))
                {
                    tempExp = result == null ? tempExp : Expression.And(result, tempExp);
                }
            }

            result = tempExp;
        }

        return result;
    }

    private Expression _GetExpressionOfGroup(GroupFilter group)
    {
        Expression result = null;

        var groupPattern = @"\[group\d\]";

        var valueString = group.Value;

        var mapFilters = Regex.Matches(group.Value, groupPattern)
            .Select(x => x as Match)
            .Select(x => new ExpressionMapFilter()
            {
                Key = x.Value,
                StartIndex = x.Index,
                EndIndex = (x.Index + x.Value.Length - 1),
            })
            .ToList();

        if (mapFilters.IsNullOrEmpty())
        {
            return null;
        }

        for (var i = 0; i < valueString.Length; i++)
        {
            if (valueString[i] == '(' || valueString[i] == ')' || !mapFilters.Any(x => x.StartIndex == i))
                continue;

            var mapFilter = mapFilters.FirstOrDefault(x => x.StartIndex == i);

            var fieldFilter = _groupFilters.FirstOrDefault(x => x.Key == mapFilter.Key);

            result = fieldFilter.Expression;

            var index = valueString.IndexOf(mapFilter.Key);

            if (index - 1 >= 0 && valueString[index - 1].ToString().Equals("!"))
            {
                result = Expression.Not(result);
            }
        }

        return result;
    }

    private Expression _GetExpressionOfCondition(GroupFilter group)
    {
        Expression result = null;

        var conditionPattern = @"\[condition\d\]";

        var valueString = group.Value;

        var mapFilters = Regex.Matches(group.Value, conditionPattern)
            .Select(x => x as Match)
            .Select(x => new ExpressionMapFilter()
            {
                Key = x.Value,
                StartIndex = x.Index,
                EndIndex = (x.Index + x.Value.Length - 1),
            })
            .ToList();

        if (mapFilters.IsNullOrEmpty())
        {
            return null;
        }

        for (var i = 0; i < valueString.Length; i++)
        {
            if (valueString[i] == '(' || valueString[i] == ')' || !mapFilters.Any(x => x.StartIndex == i))
                continue;

            var mapFilter = mapFilters.FirstOrDefault(x => x.StartIndex == i);

            var fieldFilter = _fieldFilters.FirstOrDefault(x => x.Key == mapFilter.Key);

            if (result == null)
            {
                result = fieldFilter.Expression;
            }
            else
            {
                if (mapFilter.StartIndex - 1 > 0)
                {
                    var compareOperator = valueString[mapFilter.StartIndex - 1];

                    if (compareOperator.ToString().Equals(Constants.Operator.And))
                    {
                        result = Expression.And(result, fieldFilter.Expression);
                    }
                    else if (compareOperator.ToString().Equals(Constants.Operator.Or))
                    {
                        result = Expression.Or(result, fieldFilter.Expression);
                    }
                }
            }
        }

        return result;
    }
    #endregion ==================== 5. Add Condition To Group ====================
}