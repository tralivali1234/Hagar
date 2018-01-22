using System.Collections.Generic;

namespace Hagar.Activators
{
    public class ListActivator<T> : IActivator<int, List<T>>
    {
        public List<T> Create(int arg)
        {
            return new List<T>(arg);
        }
    }
}