using System;

namespace Hagar.TypeSystem
{
    public interface ITypeFilter
    {
        bool IsPermissible(Type type);
    }
}