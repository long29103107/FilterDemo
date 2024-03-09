using FilterExpression.Directive.Implement;
using FilterExpression.Directive;
using FilterExpression.Models;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using FilterExpression.Extensions;
using FilterExpression.Constants;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http.Features;

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
            _groupFilters.Clear();
            _conditionFilters.Clear();
            _fieldFilters.Clear();
        }

        return result;
    }

    #region ==================== 1. Validate Expression ====================
    private void _ValidateFilterExpression(string fe)
    {
        List<string> feString = fe.Select(x => x.ToString()).ToList();

        var invalidChar = feString.Where(x => !Regex.IsMatch(x.Trim(), Constants.Pattern.ValidCharacter)
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
                continue;
            }

            if (character.Equals("!") || character.Equals("|") || character.Equals("&"))
            {
                indexOfSharp++;
                continue;
            }

            if (character.Equals(")"))
            {
                int length = (i - indexOfSharp) + 1;
                string tempFeFilter = fe.Substring(indexOfSharp, length);

                if (string.IsNullOrEmpty(tempFeFilter)) 
                    continue;

                AddConditionFilter(tempFeFilter);

                indexOfSharp += length + 1;
            }
        }

        foreach (var filter in _conditionFilters)
        {
            fe = fe.Replace(filter.Value, filter.Key);
        }
    }

    private void AddConditionFilter(string tempFeFilter)
    {
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

    }
    #endregion ==================== 2. Get Condition Expression ====================

    #region ==================== 3. Get Group Expression ====================
    private void _GroupFilterExpression(ref string fe)
    {
        var indexOfSharp = 0;

        var charNeedToGroup = new List<string>() 
        {
            RelationalOperator.Not,
            RelationalOperator.And,
            RelationalOperator.Or
        };

        if (string.IsNullOrEmpty(fe)) 
            return;

        _AddGroupFilterIfCaseMatches(ref indexOfSharp, ref fe);

        _GroupFilterExpression(ref fe);
    }

    private void _AddGroupFilterIfCaseMatches(ref int indexOfSharp, ref string fe)
    {
        //Case 1: Add group if condition has negation operator
        if (Regex.Matches(fe, Constants.Pattern.ConditionNot).Any())
        {
            var groups = Regex.Matches(fe, Constants.Pattern.ConditionNot)
                .Select(x => x as Match)
                .ToList();

            foreach (var group in groups)
            {
                AddGroupFilter(group.Value);
            }

            ReplaceGroupByGroupKey(ref fe);
            return;
        }

        //Case 2: Add group if character is `(`, it starts 1 group condition
        if (fe.Any(c => c.ToString().Equals("(")))
        {
            for (var i = 0; i < fe.Length; i++)
            {
                if (indexOfSharp > i)
                    continue;

                var character = fe[i].ToString();

                if (character.Equals("("))
                {
                    indexOfSharp = i;
                    continue;
                }

                if (character.Equals(")"))
                {
                    int length = (i - indexOfSharp) + 1;
                    string tempFeFilter = fe.Substring(indexOfSharp, length);

                    if (string.IsNullOrEmpty(tempFeFilter))
                        continue;

                    if (!_groupFilters.Any(x => x.Value.Equals(tempFeFilter)))
                    {
                        AddGroupFilter(fe);
                    }

                    indexOfSharp += length + 1;
                }
            }

            ReplaceGroupByGroupKey(ref fe);

            return;
        }

        //Case 3: Add group if chid group has negation operator
        if (Regex.Matches(fe, Constants.Pattern.GroupNot).Any())
        {
            var groups = Regex.Matches(fe, Constants.Pattern.GroupNot)
                .Select(x => x as Match)
                .ToList();

            foreach (var group in groups)
            {
                AddGroupFilter(fe);

                _key++;
            }

            ReplaceGroupByGroupKey(ref fe);

            return;
        }

        //Case 4: Add group if character is relational operator
        if (fe.Any(c => c.ToString().Equals("&")) || fe.Any(c => c.ToString().Equals("|")))
        {
            AddGroupFilter(fe);

            fe = string.Empty;

            return;
        }

        //Case 5: Add group if filter expression just has 1 condtion
        if (Regex.Matches(fe, Constants.Pattern.Condition).Any())
        {
            AddGroupFilter(fe);

            fe = string.Empty;

            return;
        }
        //Case 6: Replace filter expression in order to returning data
        fe = string.Empty;
    }

    private void AddGroupFilter(string fe)
    {
        var key = $"[group{_key}]";

        _groupFilters.Add(new GroupFilter
        {
            Index = _key,
            Key = key,
            Value = fe
        });

        _key++;
    }

    private void ReplaceGroupByGroupKey(ref string fe)
    {
        foreach (var groupFilter in _groupFilters)
        {
            if (fe.Contains(groupFilter.Value))
            {
                fe = fe.Replace(groupFilter.Value, groupFilter.Key);
            }
        }
    }

    #endregion ==================== 3. Get Group Expression ====================

    #region ==================== 4. Parse Filter Expression ====================
    public void _ParseFieldFilter(ref ParameterExpression pe, Type type)
    {
        foreach (var item in _conditionFilters)
        {
            if (string.IsNullOrEmpty(item.Value)) continue;

            List<string> splitStr = item.Value
                .Replace("(", "")
                .Replace(")", "")
                .Split(' ')
                .ToList();

            if (splitStr.Count != 3)
            {
                throw new Exception($"Request `{item.Value}` invalid");
            }

            string firstValue = splitStr[0] ?? string.Empty; // This is property name
            string secondValue = splitStr[1] ?? string.Empty; // This is operator
            string thirdValue = splitStr[2] ?? string.Empty; //This is value

            //Valid name field in white list
            PropertyInfo? prop = type.GetProperty(firstValue);

            if (prop == null)
            {
                throw new Exception("Property name is not exist");
            }

            var valueTypeString = prop?.PropertyType.ToTypeNameOrAlias().ToLower();

            //Valid operator in white list
            if (string.IsNullOrEmpty(secondValue))
            {
                throw new Exception("Operator must have value");
            }

            if (!_whiteListOperatior.Contains(secondValue))
            {
                throw new Exception($"Operator must be one of the keywords `{string.Join(", ", _whiteListOperatior)}` ");
            }

            //Get value in ``
            if (!thirdValue.StartsWith("`") || !thirdValue.EndsWith("`"))
            {
                throw new Exception("Value of filter must in ``");
            }

            thirdValue = thirdValue.Replace("`", "").Trim();

            //Get expression
            Expression? body = null;
            Expression? expressionName = null;

            MemberExpression me = Expression.Property(pe, firstValue);
            var typeProperty = _ParseStringToType(valueTypeString);

            if (secondValue.Equals(ComparisonOperator.In))
            {
                if (valueTypeString == "int")
                {
                    var valueType = typeof(List<int>);
                    var method = typeof(List<int>).GetMethod(nameof(List<int>.Contains), new[] { typeof(int) });

                    expressionName = Expression.Call(Expression.Constant(thirdValue.GetIntList(0), valueType),
                           method ?? throw new InvalidOperationException(), me);
                }
                else if (valueTypeString == "int?")
                {
                    var valueType = typeof(List<int?>);
                    var method = typeof(List<int?>).GetMethod(nameof(List<int?>.Contains), new[] { typeof(int?) });

                    expressionName = Expression.Call(Expression.Constant(thirdValue.GetNullableIntList(), valueType),
                           method ?? throw new InvalidOperationException(), me);
                }
                else if (valueTypeString == "string")
                {
                    var valueType = typeof(List<string>);
                    var method = typeof(List<string>).GetMethod(nameof(List<string>.Contains), new[] { typeof(string) });

                    expressionName = Expression.Call(Expression.Constant(thirdValue.GetStringList(), valueType),
                           method ?? throw new InvalidOperationException(), me);
                }
            }
            else
            {
                ConstantExpression constant = Expression.Constant(_ParseValue(thirdValue, typeProperty), typeProperty);

                expressionName = _GetGenerateExpression(me, constant, secondValue ?? string.Empty);
            }

            if (body == null || string.IsNullOrEmpty(secondValue))
            {
                body = expressionName;
            }
            else
            {
                if (secondValue == Constants.RelationalOperator.And)
                    body = Expression.And(body, expressionName);
                else if (secondValue == Constants.RelationalOperator.Or)
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
        IFilterDirective filterDirective = strOperator switch
        {
            ComparisonOperator.Contains => new ContainsDirective(),
            ComparisonOperator.Equal => new EqualDirective(),
            ComparisonOperator.GreaterThan => new GreaterThanDirective(),
            ComparisonOperator.GreaterThanAndEqual => new GreaterThanOrEqualDirective(),
            ComparisonOperator.LessThan => new LessThanDirective(),
            ComparisonOperator.LessThanAndEqual => new LessThanOrEqualDirective(),
            ComparisonOperator.NotEqual => new NotEqualDirective(),
            ComparisonOperator.StartsWith => new StartsWithDirective(),
            _ => throw new NotImplementedException(),
        };

        return filterDirective.GenerateExpression(ref me, constant);
    }

    private static Type _ParseStringToType(string strType)
    {
        if (strType == "string")
            return typeof(string);

        if (strType == "int")
            return typeof(int);

        if (strType == "decimal")
            return typeof(decimal);

        if (strType == "datetime")
            return typeof(DateTime);

        if (!string.IsNullOrEmpty(strType))
            throw new Exception($"Value Type `{strType}` is not supported yet.");

        return null;
    }

    private static object _ParseValue(string value, Type type)
    {
        var v = value.Trim();

        if (type == typeof(string)) return v;

        if (type == typeof(DateTime)) return DateTime.Parse(v);
        if (type == typeof(DateTime?)) return v.ParseNullableDateTime();

        if (type == typeof(int)) return int.Parse(v);
        if (type == typeof(int?)) return v.ParseNullableInt();

        if (type == typeof(decimal)) return decimal.Parse(v);
        if (type == typeof(decimal?)) return v.ParseNullableDecimal();

        if (type == typeof(bool)) return bool.Parse(v);
        if (type == typeof(bool?)) return v.ParseNullableBool();

        throw new Exception($"Convert value `{value}` to type `{type}` is not supported yet.");
    }

    #endregion ==================== 4. Parse Filter Expression ====================

    #region ==================== 5. Add Condition To Group ====================
    private void _AddExpressionToGroup()
    {
        foreach (var group in _groupFilters.OrderBy(x => x.Key).ToList())
        {
            var groupList = Regex.Matches(group.Value, Constants.Pattern.Group);
            var conditionList = Regex.Matches(group.Value, Constants.Pattern.Condition);

            if (conditionList.Any() && groupList.Any())
            {
                group.Expression = _GetExpressionOfConditionAndGroup(group);
                continue;
            }

            if (conditionList.Any())
            {
                group.Expression = _GetExpressionOfCondition(group);
                continue;
            }

            if (groupList.Any())
            {
                group.Expression = _GetExpressionOfGroup(group);
            }
        }
    } 

    private Expression _GetExpressionOfConditionAndGroup(GroupFilter group)
    {
        Expression result = null;

        var valueString = group.Value;

        var mapGroupFilters = _GetRegexMatches(group.Value, Constants.Pattern.Group);

        var mapConditionFilters = _GetRegexMatches(group.Value, Constants.Pattern.Condition);

        if (mapGroupFilters.IsNullOrEmpty() || mapConditionFilters.IsNullOrEmpty())
        {
            return null;
        }

        //Get group first
        foreach (var item in mapGroupFilters)
        {
            var groupFilter = _groupFilters.FirstOrDefault(x => x.Key == item.Key);

            var tempExp = _GetExpressionOfGroup(groupFilter);

            result = _AddExpressionByGroupOrCondition(valueString, item.Key, result, tempExp);
        }

        //Get condition 
        foreach (var item in mapConditionFilters)
        {
            var conditionFilter = _fieldFilters.FirstOrDefault(x => x.Key == item.Key);

            var tempExp = conditionFilter.Expression;

            result = _AddExpressionByGroupOrCondition(valueString, item.Key, result, tempExp);
        }

        return result;
    }

    private Expression _AddExpressionByGroupOrCondition(string value, string key, Expression currentExp, Expression newExp)
    {
        var index = value.IndexOf(key);

        if (index - 1 < 0) 
            return newExp;

        if (value[index - 1].ToString().Equals("!"))
            return Expression.Not(newExp);

        if(currentExp == null)
            return null;

        if (value[index - 1].ToString().Equals("|"))
            return Expression.Or(currentExp, newExp);
            
        if (value[index - 1].ToString().Equals("&"))
            return Expression.And(currentExp, newExp);

        return null;

    }

    private Expression _GetExpressionOfGroup(GroupFilter group)
    {
        Expression result = null;

        var valueString = group.Value;

        var mapFilters = _GetRegexMatches(group.Value, Constants.Pattern.Group);

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

            if(result == null)
            {
                result = fieldFilter.Expression;
            }
            else
            {
                var compareOperator = valueString[mapFilter.StartIndex - 1];

                if (compareOperator.ToString().Equals(Constants.RelationalOperator.Not))
                {
                    result = Expression.Not(result);
                    continue;
                }

                if (compareOperator.ToString().Equals(Constants.RelationalOperator.And))
                {
                    result = Expression.And(result, fieldFilter.Expression);
                    continue;
                }
                
                if (compareOperator.ToString().Equals(Constants.RelationalOperator.Or))
                {
                    result = Expression.Or(result, fieldFilter.Expression);
                }
            }
        }

        return result;
    }

    private Expression _GetExpressionOfCondition(GroupFilter group)
    {
        Expression result = null;

        var valueString = group.Value;

        var mapFilters = _GetRegexMatches(group.Value, Constants.Pattern.Condition);

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
            
            if (mapFilter.StartIndex - 1 >= 0)
            {
                var compareOperator = valueString[mapFilter.StartIndex - 1];

                if (compareOperator.ToString().Equals(Constants.RelationalOperator.Not))
                {
                    result = Expression.Not(result);
                    continue;
                }

                if (compareOperator.ToString().Equals(Constants.RelationalOperator.And))
                {
                    result = Expression.And(result, fieldFilter.Expression);
                    continue;
                }

                if (compareOperator.ToString().Equals(Constants.RelationalOperator.Or))
                {
                    result = Expression.Or(result, fieldFilter.Expression);
                }
            }
        }

        return result;
    }

    private List<ExpressionMapFilter> _GetRegexMatches(string value, string pattern)
    {
        var result = Regex.Matches(value, pattern)
           .Select(x => x as Match)
           .Select(x => new ExpressionMapFilter()
           {
               Key = x.Value,
               StartIndex = x.Index,
               EndIndex = (x.Index + x.Value.Length - 1),
           })
           .ToList();

        return result;
    }

    #endregion ==================== 5. Add Condition To Group ====================
}