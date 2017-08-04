using System;
using System.Collections.Generic;
using Hagar.WireProtocol;

namespace Hagar
{
    public class WellKnownTypeCollection
    {
        private readonly Dictionary<uint, Type> wellKnownTypes;
        private readonly Dictionary<Type, uint> wellKnownTypeToIdMap = new Dictionary<Type, uint>();
        public WellKnownTypeCollection()
        {
            this.wellKnownTypes = new Dictionary<uint, Type>
            {
                [0] = typeof(void), // Represents the type of null
                [1] = typeof(int),
                [2] = typeof(string),
                [3] = typeof(bool),
                [4] = typeof(short),
                [5] = typeof(long),
                [6] = typeof(sbyte),
                [7] = typeof(uint),
                [8] = typeof(ushort),
                [9] = typeof(ulong),
                [10] = typeof(byte),
                [11] = typeof(float),
                [12] = typeof(double),
                [13] = typeof(decimal),
                [14] = typeof(char),
                [15] = typeof(Guid),
                [16] = typeof(DateTime),
                [17] = typeof(TimeSpan),
            };
            foreach (var item in this.wellKnownTypes)
            {
                this.wellKnownTypeToIdMap[item.Value] = item.Key;
            }
        }
        public Type GetWellKnownType(uint typeId) => this.wellKnownTypes[typeId];
        public bool TryGetWellKnownType(uint typeId, out Type type) => this.wellKnownTypes.TryGetValue(typeId, out type);
        public bool TryGetWellKnownTypeId(Type type, out uint typeId) => this.wellKnownTypeToIdMap.TryGetValue(type, out typeId);
    }

    public class ReferencedTypeCollection
    {
        private readonly Dictionary<uint, Type> referencedTypes = new Dictionary<uint, Type>();
        private readonly Dictionary<Type, uint> referencedTypeToIdMap = new Dictionary<Type, uint>();

        public Type GetReferencedType(uint reference) => this.referencedTypes[reference];
        public bool TryGetReferencedType(uint reference, out Type type) => this.referencedTypes.TryGetValue(reference, out type);
        public bool TryGetTypeReference(Type type, out uint reference) => this.referencedTypeToIdMap.TryGetValue(type, out reference);
    }
    public class SerializationContext
    {
        public TypeCodec TypeCodec { get; } = new TypeCodec();
        public WellKnownTypeCollection WellKnownTypes { get; } = new WellKnownTypeCollection();
        public ReferencedTypeCollection ReferencedTypes { get; } = new ReferencedTypeCollection();

        public Type GetWellKnownType(uint typeId) => this.WellKnownTypes.GetWellKnownType(typeId);
        public bool TryGetWellKnownType(uint typeId, out Type type) => this.WellKnownTypes.TryGetWellKnownType(typeId, out type);
        public bool TryGetWellKnownTypeId(Type type, out uint typeId) => this.WellKnownTypes.TryGetWellKnownTypeId(type, out typeId);

        public Type GetReferencedType(uint reference) => this.ReferencedTypes.GetReferencedType(reference);
        public bool TryGetReferencedType(uint reference, out Type type) => this.ReferencedTypes.TryGetReferencedType(reference, out type);
        public bool TryGetTypeReference(Type type, out uint reference) => this.ReferencedTypes.TryGetTypeReference(type, out reference);
    }

    public interface IToken
    {
        
    }

    public class StartObject : IToken
    {
        private readonly Field field;

        public StartObject(Field field)
        {
            this.field = field;
        }

        public override string ToString()
        {
            return $"StartObject({nameof(field)}: {field})";
        }
    }
    public class EndObject : IToken
    {
        private readonly Field field;

        public EndObject(Field field)
        {
            this.field = field;
        }

        public override string ToString()
        {
            return $"EndObject({nameof(field)}: {field})";
        }
    }

    public class Parser
    {
        public IEnumerable<IToken> Parse(Reader reader, SerializationContext context)
        {
            var depth = 0;
            do
            {
                var field = reader.ReadFieldHeader(context);
                if (field.IsEndObject)
                {
                    depth--;
                    yield return new EndObject(field);
                }
                else if (field.IsStartObject)
                {
                    depth++;
                    yield return new StartObject(field);
                }
                else if (field.HasFieldId)
                {
                    
                }

            } while (depth > 0);
            yield break;
        }
    }
}