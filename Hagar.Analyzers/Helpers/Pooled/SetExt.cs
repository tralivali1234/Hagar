using System.Collections.Generic;

namespace Hagar.Analyzers.Helpers.Pooled
{
    internal static class SetExt
    {
        internal static bool? AddIfNotNull<T>(this HashSet<T> set, T item)
        {
            if (item != null)
            {
                return set.Add(item);
            }

            return null;
        }
    }
}