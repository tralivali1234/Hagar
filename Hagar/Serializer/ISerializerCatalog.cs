using System;
using System.Collections.Generic;
using Hagar.Codec;

namespace Hagar.Serializer
{
    public interface ISerializerCatalog
    {
        IFieldCodec<object> GetSerializer(Type fieldType);
    }

    public class SerializerCatalog : ISerializerCatalog
    {
        private Dictionary<Type, IFieldCodec<object>> serializers;

        public SerializerCatalog(Dictionary<Type, IFieldCodec<object>> serializers)
        {
            this.serializers = serializers;
        }

        public void SetSerializer(Dictionary<Type, IFieldCodec<object>> newOnes) => this.serializers = newOnes;

        public IFieldCodec<object> GetSerializer(Type fieldType) => this.serializers[fieldType];
    }
}