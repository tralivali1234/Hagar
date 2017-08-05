namespace Hagar.Session
{
    public class SerializationContext
    {
        public TypeCodec TypeCodec { get; } = new TypeCodec();
        public WellKnownTypeCollection WellKnownTypes { get; } = new WellKnownTypeCollection();
        public ReferencedTypeCollection ReferencedTypes { get; } = new ReferencedTypeCollection();
        public ReferencedObjectCollection ReferencedObjects { get; } = new ReferencedObjectCollection();

        public void Reset()
        {
            this.ReferencedObjects.Reset();
        }
    }
}