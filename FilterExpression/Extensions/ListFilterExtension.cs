using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterExpression.Extensions;

public static class ListFilterExtension
{
    private static FilterService _filterService = new FilterService();
    
    /// <summary>
    /// Check list is null or empty
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="fe"></param>
    /// <returns></returns>
    public static List<T> Filter<T>(this List<T> list, string fe)
    {
        var filter = _filterService.Filter<T>(fe);

        if(filter != null)
        {
            return list.Where(filter.Compile()).ToList();
        }

        return list.ToList();
    }

    /// <summary>
    /// Determines whether the collection is null or contains no elements.
    /// </summary>
    /// <typeparam name="T">The IEnumerable type.</typeparam>
    /// <param name="enumerable">The enumerable, which may be null or empty.</param>
    /// <returns>
    ///     <c>true</c> if the IEnumerable is null or empty; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable == null)
        {
            return true;
        }
        /* If this is a list, use the Count property for efficiency. 
         * The Count property is O(1) while IEnumerable.Count() is O(N). */
        var collection = enumerable as ICollection<T>;
        if (collection != null)
        {
            return collection.Count < 1;
        }
        return !enumerable.Any();
    }
}
