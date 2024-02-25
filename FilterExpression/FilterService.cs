using FilterExpression.Directive;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FilterExpression
{
    public class ConditionFilter
    {
        public int Index { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        public Expression<Func<T, bool>> GetExpressionFunction<T>(List<FieldFilter> fieldFilters) where T : class
        {
            Expression<Func<T, bool>> result = null;
            var operationFilter = fieldFilters.FirstOrDefault(x => x.Key ==  Key);

            return result;
        }
    }

    public class GroupFilter
    {
        public int Index { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public partial class FilterService
    {
        private List<ConditionFilter> _conditionFilters = new List<ConditionFilter>();
        private List<GroupFilter> _groupFilters = new List<GroupFilter>();
        private List<FieldFilter> _fieldFilters = new List<FieldFilter>();
        private int _groupKey = 0;
        private int _conditionKey = 0;

        public Expression<Func<T, bool>> GetExpressionFilterRecursive<T>()
        {
            Expression<Func<T, bool>> result = null;
            
            return result;
        }

        public Expression<Func<T, bool>> Filter<T>(string fe)
        {
            Expression<Func<T, bool>> result = null;

            if (string.IsNullOrEmpty(fe))
            {
                return result;
            }

            _ValidateFilterExpression(fe);

            _GroupFilterExpression(fe);

            _ParseFieldFilter(typeof(T));

            result = GetExpressionFilterRecursive<T>();

            _groupKey = 0;
            _conditionKey = 0;
            _groupFilters.Clear();
            _conditionFilters.Clear();

            return result;
        }

        private void _GroupFilterExpression(string fe)
        {
            string tempFe = fe;

            if (!fe.Contains(")") || !fe.Contains("("))
            {
                _groupFilters.Add(new GroupFilter
                {
                    Index = _groupKey,
                    Key = $"group{_groupKey++}",
                    Value = fe
                });

                return;
            }

            //index of `(`
            var indexOfSharp = 0;
            var inSharp = 0;

            for (var i = 0; i < tempFe.Length; i++)
            {
                if (fe[i].ToString().Equals("!"))
                {
                    inSharp = i;
                }
                else if (fe[i].ToString().Equals("("))
                {
                    indexOfSharp = i;
                }
                else if (fe[i].ToString().Equals(")"))
                {
                    int length = (i - indexOfSharp) + 1;

                    string tempFeFilter = tempFe.Substring(indexOfSharp, length);

                    if (inSharp != 0 && Math.Abs(indexOfSharp - inSharp) == 1)
                    {
                        tempFeFilter = tempFe.Substring(inSharp, i);
                    }

                    if (tempFeFilter.Contains("condition"))
                    {
                        _groupFilters.Add(new GroupFilter
                        {
                            Index = _groupKey,
                            Key = $"[group{_groupKey++}]",
                            Value = tempFeFilter
                        });
                    }
                    else
                    {
                        if (!tempFeFilter.Contains(tempFe))
                        {
                            _conditionFilters.Add(new ConditionFilter
                            {
                                Index = _groupKey,
                                Key = $"[condition{_groupKey++}]",
                                Value = tempFeFilter
                            });
                        }
                    }

                    indexOfSharp = 0;
                    inSharp = 0;
                }
            }

            foreach (var group in _groupFilters)
            {
                tempFe = tempFe.Replace(group.Value, group.Key);
            }

            foreach (var group in _conditionFilters)
            {
                tempFe = tempFe.Replace(group.Value, group.Key);
            }

            _GroupFilterExpression(tempFe);
        }


        private void _ValidateFilterExpression(string fe)
        {
            List<string> feString = fe.Select(x => x.ToString()).ToList();
            List<string> validChar = new List<string>() { "!", "", " ", "`", "(", ")", "|", "%", };
            var pattern = "^[A-Za-z0-9_.]+$";

            var invalidChar = feString.Where(x => !Regex.IsMatch(x.Trim(), pattern) && !validChar.Contains(x.Trim()))
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
    }
}