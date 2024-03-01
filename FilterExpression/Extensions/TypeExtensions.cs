using FilterExpression.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterExpression.Extensions;
public static class TypeExtensions
{
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
}
