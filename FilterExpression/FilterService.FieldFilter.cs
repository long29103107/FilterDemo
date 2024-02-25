using FilterExpression.Directive.Implement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FilterExpression;
public partial class FilterService
{

    // This is the set of types from the C# keyword list.
    static Dictionary<Type, string> _typeAlias = new Dictionary<Type, string>
    {
        { typeof(bool), "bool" },
        { typeof(byte), "byte" },
        { typeof(char), "char" },
        { typeof(decimal), "decimal" },
        { typeof(double), "double" },
        { typeof(float), "float" },
        { typeof(int), "int" },
        { typeof(long), "long" },
        { typeof(object), "object" },
        { typeof(sbyte), "sbyte" },
        { typeof(short), "short" },
        { typeof(string), "string" },
        { typeof(uint), "uint" },
        { typeof(ulong), "ulong" },
        // Yes, this is an odd one.  Technically it's a type though.
        { typeof(void), "void" }
    };

    static string TypeNameOrAlias(Type type)
    {
        // Lookup alias for type
        if (_typeAlias.TryGetValue(type, out string alias))
            return alias;

        // Default to CLR type name
        return type.Name;
    }

    private List<string> _whiteListOperatior = new List<string>() 
    {
        "contains", "eq", "gt", "ge", "in", "lt", "le", "ne", "startswith"
    };

    //private static object _ParseValue( string value, Type type)
    //{
    //    var v = value.Trim();

    //    if (type == typeof(string)) return v;

    //    if (type == typeof(DateTime)) return DateTime.Parse(v);
    //    if (type == typeof(DateTime?)) return v.ParseNullableDateTime();

    //    if (type == typeof(int)) return int.Parse(v);
    //    if (type == typeof(int?)) return v.ParseNullableInt();

    //    if (type == typeof(decimal)) return decimal.Parse(v);
    //    if (type == typeof(decimal?)) return v.ParseNullableDecimal();

    //    if (type == typeof(bool)) return filterSetting.IsNot ? !bool.Parse(v) : bool.Parse(v);
    //    if (type == typeof(bool?)) return filterSetting.IsNot ? !v.ParseNullableBool() : v.ParseNullableBool();

    //    throw new Exception($"Convert value `{value}` to type `{type}` is not supported yet.");
    //}

    public void _ParseFieldFilter(Type type)
    {
        foreach(var item in _conditionFilters)
        {
            if (string.IsNullOrEmpty(item.Value)) continue;

            List<string> splitStr = item.Value.Replace("(","").Replace(")","").Split(' ').ToList();

            if(splitStr.Count != 3)
            {
                throw new Exception($"Request `{item.Value}` invalid");
            }

            var firstValue = splitStr[0];
            var secondValue = splitStr[1];
            var thirdValue = splitStr[2];

            //TODO: valid name field in white list
            PropertyInfo? prop = type.GetProperty(firstValue);

            if (prop == null)
                throw new Exception("Property name is not exist");

            var valueTypeString = TypeNameOrAlias(prop?.PropertyType).ToLower();

            //TODO: valid operator in white list
            if(string.IsNullOrEmpty(secondValue))
            {
                throw new Exception("Operator must have value");
            }

            if(!_whiteListOperatior.Contains(secondValue))
            {
                throw new Exception($"Operator must be one of the keywords `{string.Join(", ", _whiteListOperatior)}` ");
            }

            //TODO: get value value type
            if (!thirdValue.StartsWith("`") || !thirdValue.EndsWith("`"))
            {
                throw new Exception("Value of filter must in ``");
            }

            thirdValue = thirdValue.Replace("`", "").Trim();

            var fieldFilter = new FieldFilter()
            {
                FieldName = firstValue,
                Operator = secondValue,
                Value = thirdValue,
                Exp = item.Value,
                Key = item.Key,
                ValueTypeString = valueTypeString
            };

            _fieldFilters.Add(fieldFilter);
        }
    }
}

public class FieldFilter
{
    public string Key { get; set; }
    public string FieldName { get; set; }
    public string Operator { get; set; }
    public string Value { get; set; }
    public string ValueTypeString { get; set; }
    public string Exp { get; set; }
    public Type ValueType
    {
        get
        {
            if (ValueTypeString == "string")
            {
                return typeof(string);
            }
            else if (ValueTypeString == "int")
            {
                return typeof(int);
            }
            else if (ValueTypeString == "decimal")
            {
                return typeof(decimal);
            }
            else if (ValueTypeString == "datetime")
            {
                return typeof(DateTime);
            }
            else if (!string.IsNullOrEmpty(ValueTypeString))
            {
                throw new Exception($"Value Type `{ValueTypeString}` is not supported yet.");
            }

            return null;
        }
    }
}