using FilterExpression.Extension;
using FilterExpression.Extensions;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace FilterExpression
{
    public class ExpressionFilter
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

    public class ConditionFilter
    {
        public int Index { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
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
        private List<ExpressionFilter> _fieldFilters = new List<ExpressionFilter>();
        private int _groupKey = 0;
        private int _conditionKey = 0;

        public Expression<Func<T, bool>> GetExpressionFilterRecursive<T>(ref ParameterExpression pe)
        {
            var groupMaster = _groupFilters.OrderByDescending(x => x.Key).FirstOrDefault();

            if (groupMaster == null)
                return null;

            Expression body = null;

            body = GetExpressionRecursive(groupMaster, body);

            return body == null ? null : Expression.Lambda<Func<T, bool>>(body, pe);
        }

        private Expression GetExpressionRecursive(GroupFilter group, Expression body = null)
        {
            var pattern = @"\[group\d\]";

            var listSplit = Regex.Matches(group.Value, pattern);

            Expression tmpExp = null;

            if (listSplit.Any())
            {
                foreach (Match splitGroup in listSplit)
                {
                    var index = group.Value.IndexOf(splitGroup.Value);

                    var groupItem = _groupFilters.Where(x => x.Key == splitGroup.Value).FirstOrDefault();

                    tmpExp = GetExpressionRecursive(groupItem, body);
                }
            }
            else
            {
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

                for(var i = 0;  i < valueString.Length; i++) 
                {
                    if (valueString[i] == '(' || valueString[i] == ')' || !mapFilters.Any(x => x.StartIndex == i))
                        continue;

                    var mapFilter = mapFilters.FirstOrDefault(x => x.StartIndex == i);

                    var fieldFilter = _fieldFilters.FirstOrDefault(x => x.Key == mapFilter.Key);

                    if(body == null)
                    {
                        body = fieldFilter.Expression;
                    }
                    else
                    {
                        if (mapFilter.StartIndex - 1 > 0)
                        {
                            var compareOperator = valueString[mapFilter.StartIndex - 1];

                            if (compareOperator.ToString().Equals(Operator.And))
                            {
                                body = Expression.And(body, fieldFilter.Expression);
                            }
                            else if (compareOperator.ToString().Equals(Operator.Or))
                            {
                                body = Expression.Or(body, fieldFilter.Expression);
                            }
                        }
                    }
                }
            }

            return listSplit.Any() ? tmpExp : body;
        }

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
                _ValidateFilterExpression(tempFe);

                _ConditionFilterExpression(ref tempFe);

                _GroupFilterExpression(ref tempFe);

                _ParseFieldFilter(ref pe, typeOfGeneric);

                result = GetExpressionFilterRecursive<T>(ref pe);

                var aa = 1;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _groupKey = 0;
                _conditionKey = 0;
                _groupFilters.Clear();
                _conditionFilters.Clear();
            }

            return result;
        }

        private void _GroupFilterExpression(ref string fe)
        {
            var indexOfSharp = 0;
            var pattern = @"\!\[group\d\]";

            var charNeedToGroup = new List<string>() { "!", "|", "&" };

            if (string.IsNullOrEmpty(fe)) return;

            //var validToGroup = fe.Where(c => charNeedToGroup.Any(y => c.ToString().Equals(y))).ToList();

            //if (validToGroup.IsNullOrEmpty())
            //{
            //    return;
            //}

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
                            var key = $"[group{_groupKey}]";
                            _groupFilters.Add(new GroupFilter
                            {
                                Index = _groupKey,
                                Key = key,
                                Value = tempFeFilter
                            });

                            _groupKey++;
                        }

                        indexOfSharp += length + 1;
                    }
                    else
                    {
                        continue;
                    }
                }

                foreach(var groupFilter in _groupFilters)
                {
                    if(fe.Contains(groupFilter.Value))
                    {
                        fe = fe.Replace(groupFilter.Value, groupFilter.Key);
                    }
                }
            }
            else if (Regex.Matches(fe, pattern).Any())
            {
                var groups = Regex.Matches(fe, pattern).Select(x => x as Match).ToList();

                foreach(var group in groups)
                {
                    var key = $"[group{_groupKey}]";
                    _groupFilters.Add(new GroupFilter
                    {
                        Index = _groupKey,
                        Key = key,
                        Value = group.Value
                    });

                    _groupKey++;
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
                var key = $"[group{_groupKey}]";
                _groupFilters.Add(new GroupFilter
                {
                    Index = _groupKey,
                    Key = key,
                    Value = fe
                });

                fe = string.Empty;
            }
           
            _GroupFilterExpression(ref fe);
        }

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
                        var key = $"[condition{_conditionKey++}]";
                        _conditionFilters.Add(new ConditionFilter
                        {
                            Index = _conditionKey,
                            Key = key,
                            Value = tempFeFilter
                        });
                    } 

                    indexOfSharp += length + 1;
                }
            }

            foreach(var filter in _conditionFilters)
            {
                fe = fe.Replace(filter.Value, filter.Key);
            }
        }

        //private void _ConditionFilterExpression(string fe)
        //{
        //    string tempFe = fe;

        //    //if (!tempFe.Contains(")") || !tempFe.Contains("("))
        //    //{
        //    //    _groupFilters.Add(new GroupFilter
        //    //    {
        //    //        Index = _groupKey,
        //    //        Key = $"group{_groupKey++}",
        //    //        Value = fe
        //    //    });

        //    //    return;
        //    //}

        //    //index of `(`
        //    var indexOfSharp = 0;
        //    var inSharp = 0;

        //    for (var i = 0; i < tempFe.Length; i++)
        //    {
        //        if (indexOfSharp > i) continue;

        //        if (tempFe[i].ToString().Equals("!"))
        //        {
        //            inSharp = i;
        //        }
        //        else if (tempFe[i].ToString().Equals("("))
        //        {
        //            indexOfSharp = i;
        //        }
        //        else if (tempFe[i].ToString().Equals(")"))
        //        {
        //            int length = (i - indexOfSharp) + 1;

        //            string tempFeFilter = tempFe.Substring(indexOfSharp, length);

        //            if (inSharp != 0 && Math.Abs(indexOfSharp - inSharp) == 1)
        //            {
        //                tempFeFilter = tempFe.Substring(inSharp, i);
        //            }

        //            //if (tempFeFilter.Contains("condition"))
        //            //{
        //            //    _groupFilters.Add(new GroupFilter
        //            //    {
        //            //        Index = _groupKey,
        //            //        Key = $"[group{_groupKey++}]",
        //            //        Value = tempFeFilter
        //            //    });
        //            //}
        //            //else
        //            //{
        //            if (!tempFeFilter.Contains(tempFe))
        //            {
        //                var key = $"[condition{_conditionKey++}]";
        //                _conditionFilters.Add(new ConditionFilter
        //                {
        //                    Index = _conditionKey,
        //                    Key = key,
        //                    Value = tempFeFilter
        //                });

        //                indexOfSharp += length;
        //                tempFe = tempFe.Replace(tempFeFilter, key);
        //            }
        //            //}
        //        }
        //    }

        //    foreach (var group in _groupFilters)
        //    {
        //        tempFe = tempFe.Replace(group.Value, group.Key);
        //    }

        //    //foreach (var group in _conditionFilters)
        //    //{
        //    //    tempFe = tempFe.Replace(group.Value, group.Key);
        //    //}

        //    _ConditionFilterExpression(tempFe);
        //}


        private void _ValidateFilterExpression(string fe)
        {
            List<string> feString = fe.Select(x => x.ToString()).ToList();
            List<string> validChar = new List<string>() { "!", "", " ", "`", "(", ")", "|", "%", "&", };
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