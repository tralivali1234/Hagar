using System;
using System.Collections.Generic;

namespace Hagar.Metadata
{
    public class CodecMetadata
    {
        public HashSet<Type> FieldCodecs { get; } = new HashSet<Type>();

        public HashSet<Type> PartialSerializers { get; } = new HashSet<Type>();
    }
}