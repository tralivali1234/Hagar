using System.Collections.Generic;

namespace Hagar.Analyzers.Helpers.Pooled
{
    internal static class SetPool<T>
    {
        private static readonly Pool<HashSet<T>> Pool = new Pool<HashSet<T>>(() => new HashSet<T>(), x => x.Clear());

        public static Pool<HashSet<T>>.Pooled Create()
        {
            return Pool.GetOrCreate();
        }
    }
}