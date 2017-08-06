using System;
using Hagar.Codec;
using Hagar.Session;

namespace Hagar.TestKit
{
    public abstract class FieldCodecTester<TField, TCodec> where TCodec : IFieldCodec<TField>
    {

        public void CorrectlyAdvancesReferenceCounter(TCodec codec)
        {
            var session = new SerializerSession();
        }
    }
}
