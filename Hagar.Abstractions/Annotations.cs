using System;

namespace Hagar
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public sealed class GenerateSerializerAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public sealed class TypeIdAttribute : Attribute
    {
        public TypeIdAttribute(int id)
        {
            this.Id = id;
        }

        public int Id { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class FieldIdAttribute : Attribute
    {
        public FieldIdAttribute(int id)
        {
            this.Id = id;
        }

        public int Id { get; }
    }
}
