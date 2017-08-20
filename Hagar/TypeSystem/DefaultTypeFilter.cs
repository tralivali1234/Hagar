using System;

namespace Hagar.TypeSystem
{
    public class DefaultTypeFilter : ITypeFilter
    {
#warning this is a potential security risk and must be changed before release.
        public bool IsPermissible(Type type) => true;
    }
}