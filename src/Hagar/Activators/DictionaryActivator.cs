using System.Collections.Generic;

namespace Hagar.Activators
{
    public class DictionaryActivator<TKey, TValue> : IActivator<IEqualityComparer<TKey>, Dictionary<TKey, TValue>>
    {
        public Dictionary<TKey, TValue> Create(IEqualityComparer<TKey> arg)
        {
            return new Dictionary<TKey, TValue>(arg);
        }
    }
}