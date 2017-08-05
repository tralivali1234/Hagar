using Hagar.Session;
using Hagar.Utilities;

namespace Hagar.Activator
{
    public class DefaultActivator<T> : IActivator<T>
    {
        public T Create(Reader reader, SerializationContext context)
        {
            return System.Activator.CreateInstance<T>();
        }
    }
}