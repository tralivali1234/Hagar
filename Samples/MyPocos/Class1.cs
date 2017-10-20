using System;
using System.Collections.Generic;
using Hagar;

namespace MyPocos
{
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
}
