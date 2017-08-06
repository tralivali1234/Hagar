using System;
using System.Collections.Generic;
using Hagar.Codec;

namespace Hagar.Serializer
{
    public interface ICodecProvider
    {
        IFieldCodec<object> GetCodec(Type fieldType);
    }

    public interface IGenericCodec : IFieldCodec<object>
    {
        bool IsSupportedType(Type type);
    }

    public class CodecProvider : ICodecProvider
    {
        private readonly Dictionary<Type, IFieldCodec<object>> serializers;
        private readonly List<IGenericCodec> genericCodecs;
        
        public CodecProvider(Dictionary<Type, IFieldCodec<object>> serializers, List<IGenericCodec> genericCodecs)
        {
            this.serializers = serializers;
            this.genericCodecs = genericCodecs;
        }
        
        public IFieldCodec<object> GetCodec(Type fieldType)
        {
            if (this.serializers.TryGetValue(fieldType, out IFieldCodec<object> result)) return result;
            foreach (var codec in this.genericCodecs)
            {
                if (codec.IsSupportedType(fieldType)) return codec;
            }

            // TODO: Use a library-specific exception.
            throw new KeyNotFoundException($"Could not find a codec for type {fieldType}.");
        }
    }
}