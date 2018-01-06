using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal class SerializerFieldDescription
    {
        public SerializerFieldDescription(ITypeSymbol fieldType, string fieldName)
        {
            this.FieldType = fieldType;
            this.FieldName = fieldName;
        }

        public ITypeSymbol FieldType { get; }
        public string FieldName { get; }
    }
}