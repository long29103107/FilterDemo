using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterExpression.Extensions;
public static class TypeExtensions
{
    public static Dictionary<Type, string> _typeAlias = new Dictionary<Type, string>
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

    public static Type ToType(this string strType)
    {
        Type? result = strType switch
        {
            "string" => typeof(string),
            "int" => typeof(int),
            "decimal" => typeof(decimal),
            "datetime" => typeof(DateTime),
            _ => null
        };

        if (result == null && !string.IsNullOrEmpty(strType))
        {
            throw new Exception($"Value Type `{strType}` is not supported yet.");
        }

        return result;
    }

    public static string ToTypeNameOrAlias(this Type type)
    {
        // Lookup alias for type
        if (_typeAlias.TryGetValue(type, out string alias))
            return alias;

        // Default to CLR type name
        return type.Name;
    }
}
