namespace Hagar.Session
{
    public class SerializationContext
    {
        public TypeCodec TypeCodec { get; } = new TypeCodec();
        public WellKnownTypeCollection WellKnownTypes { get; } = new WellKnownTypeCollection();
        public ReferencedTypeCollection ReferencedTypes { get; } = new ReferencedTypeCollection();
    }
}