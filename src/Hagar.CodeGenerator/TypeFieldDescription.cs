using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal class TypeFieldDescription : SerializerFieldDescription
    {
        public TypeFieldDescription(ITypeSymbol fieldType, string fieldName, ITypeSymbol underlyingType) : base(fieldType, fieldName)
        {
            this.UnderlyingType = underlyingType;
        }

        public ITypeSymbol UnderlyingType { get; }
    }
}