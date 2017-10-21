using Hagar.TypeSystem;

namespace Hagar.Session
{
    public sealed class SerializerSession
    {
        public SerializerSession(
            TypeCodec typeCodec,
            WellKnownTypeCollection wellKnownTypes,
            ReferencedTypeCollection referencedTypes,
            ReferencedObjectCollection referencedObjects)
        {
            this.TypeCodec = typeCodec;
            this.WellKnownTypes = wellKnownTypes;
            this.ReferencedTypes = referencedTypes;
            this.ReferencedObjects = referencedObjects;
        }

        public TypeCodec TypeCodec { get; }
        public WellKnownTypeCollection WellKnownTypes { get; }
        public ReferencedTypeCollection ReferencedTypes { get; }
        public ReferencedObjectCollection ReferencedObjects { get; }

        public void PartialReset()
        {
            this.ReferencedObjects.Reset();
        }
    }
}