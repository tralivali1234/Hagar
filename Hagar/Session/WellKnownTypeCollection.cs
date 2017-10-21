using System;
using System.Collections.Generic;

namespace Hagar.Session
{
    public sealed class WellKnownTypeCollection
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

        public Type GetWellKnownType(uint typeId)
        {
            if (typeId == 0) return null;
            return this.wellKnownTypes[typeId];
        }

        public bool TryGetWellKnownType(uint typeId, out Type type)
        {
            if (typeId == 0)
            {
                type = null;
                return true;
            }

            return this.wellKnownTypes.TryGetValue(typeId, out type);
        }

        public bool TryGetWellKnownTypeId(Type type, out uint typeId)
        {
            if (type == null)
            {
                typeId = 0;
                return true;
            }

            return this.wellKnownTypeToIdMap.TryGetValue(type, out typeId);
        }
    }
}