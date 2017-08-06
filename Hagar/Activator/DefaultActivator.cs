using Hagar.Session;
using Hagar.Utilities;

namespace Hagar.Activator
{
    public class DefaultActivator<T> : IActivator<T>
    {
        public T Create(Reader reader, Session.SerializerSession context)
        {
            return System.Activator.CreateInstance<T>();
        }
    }
}