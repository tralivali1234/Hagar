using Hagar.Analyzers.Helpers.KnownSymbols.BaseTypes;

namespace Hagar.Analyzers.Helpers.KnownSymbols
{
    // ReSharper disable once InconsistentNaming
    internal class XmlSerializerType : QualifiedType
    {
        public XmlSerializerType()
            : base("System.Xml.Serialization.XmlSerializer")
        {
        }
    }
}