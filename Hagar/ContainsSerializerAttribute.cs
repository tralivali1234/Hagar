using System;

namespace Hagar
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ContainsSerializerAttribute : Attribute
    {
    }
}
