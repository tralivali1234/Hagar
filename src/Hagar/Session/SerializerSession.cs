using System;
using Hagar.TypeSystem;

namespace Hagar.Session
{
    public sealed class SerializerSession : IDisposable
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

        internal Action<SerializerSession> OnDisposed { get; set; }

        public void PartialReset()
        {
            this.ReferencedObjects.Reset();
        }

        public void FullReset()
        {
            this.ReferencedObjects.Reset();
            this.ReferencedTypes.Reset();
        }

        public void Dispose() => this.OnDisposed?.Invoke(this);
    }
}