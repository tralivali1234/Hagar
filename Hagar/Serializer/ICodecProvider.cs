using System;
using System.Collections.Generic;
using Hagar.Codec;

namespace Hagar.Serializer
{
    public interface ICodecProvider
    {
        IFieldCodec<object> GetCodec(Type fieldType);
    }

    public interface ITypedCodecProvider : ICodecProvider
    {
        IFieldCodec<TField> GetCodec<TField>();
    }

    public interface ICodecWrapper
    {
        object InnerCodec { get; }
    }

    public interface IGenericCodec : IFieldCodec<object>
    {
        bool IsSupportedType(Type type);
    }

    public class CodecProvider : ITypedCodecProvider
    {
        private readonly Dictionary<Type, IFieldCodec<object>> serializers;
        private readonly List<IGenericCodec> genericCodecs;
        private readonly VoidCodec voidCodec = new VoidCodec();

        public CodecProvider(Dictionary<Type, IFieldCodec<object>> serializers, List<IGenericCodec> genericCodecs)
        {
            this.serializers = serializers;
            this.genericCodecs = genericCodecs;
        }

        public IFieldCodec<object> GetCodec(Type fieldType)
        {
            if (fieldType == null) return this.voidCodec;
            if (this.serializers.TryGetValue(fieldType, out IFieldCodec<object> result)) return result;
            foreach (var codec in this.genericCodecs)
            {
                if (codec.IsSupportedType(fieldType)) return codec;
            }

            // TODO: Use a library-specific exception.
            throw new KeyNotFoundException($"Could not find a codec for type {fieldType}.");
        }

        public IFieldCodec<TField> GetCodec<TField>()
        {
            var codec = this.GetCodec(typeof(TField));
            if (codec is IFieldCodec<TField> typedResult) return typedResult;
            if (codec is ICodecWrapper wrapper && wrapper.InnerCodec is IFieldCodec<TField> wrapped) return wrapped;
            return new TypedCodecWrapper<TField>(codec);
        }
    }
}