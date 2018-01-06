using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal class CodecFieldDescription : SerializerFieldDescription
    {
        public CodecFieldDescription(ITypeSymbol fieldType, string fieldName, ITypeSymbol underlyingType) : base(fieldType, fieldName)
        {
            this.UnderlyingType = underlyingType;
        }

        public ITypeSymbol UnderlyingType { get; }
    }
}