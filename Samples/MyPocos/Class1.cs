using System;
using System.Collections.Generic;
using Hagar;

namespace MyPocos
{
    public class X
    {
        public void S()
        {
            throw new FieldAccessException();
        }
    }

    [GenerateSerializer]
    public class SomeClassWithSerialzers
    {
        [FieldId(0)]
        public int IntProperty { get; set; }

        [FieldId(1)] public int IntField;

        public int UnmarkedField;

        public int UnmarkedProperty { get; set; }
    }

    [GenerateSerializer]
    public class SerializableClassWithCompiledBase : List<int>
    {
        [FieldId(0)]
        public int IntProperty { get; set; }
    }


    [GenerateSerializer]
    public class GenericPoco<T>
    {
        [FieldId(0)]
        public T Field { get; set; }

        [FieldId(1030)]
        public T[] ArrayField { get; set; }

        [FieldId(2222)]
        public Dictionary<T, T> DictField { get; set; }
    }
}
