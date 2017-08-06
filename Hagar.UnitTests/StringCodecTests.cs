using System;
using Hagar.Codec;
using Hagar.TestKit;

namespace Hagar.UnitTests
{
    public class StringCodecTests : FieldCodecTester<string, StringCodec>
    {
        protected override StringCodec CreateCodec() => new StringCodec();

        protected override string CreateValue() => Guid.NewGuid().ToString();
    }
}
