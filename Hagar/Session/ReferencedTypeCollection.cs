using System;
using System.Collections.Generic;

namespace Hagar.Session
{
    public class ReferencedTypeCollection
    {
        private readonly Dictionary<uint, Type> referencedTypes = new Dictionary<uint, Type>();
        private readonly Dictionary<Type, uint> referencedTypeToIdMap = new Dictionary<Type, uint>();

        public Type GetReferencedType(uint reference) => this.referencedTypes[reference];
        public bool TryGetReferencedType(uint reference, out Type type) => this.referencedTypes.TryGetValue(reference, out type);
        public bool TryGetTypeReference(Type type, out uint reference) => this.referencedTypeToIdMap.TryGetValue(type, out reference);
    }
}