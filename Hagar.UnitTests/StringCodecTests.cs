using System;
using Hagar.Codec;
using Hagar.TestKit;
using Xunit;

namespace Hagar.UnitTests
{
    public class StringCodecTests : FieldCodecTester<string, StringCodec>
    {
        protected override StringCodec CreateCodec() => new StringCodec();

        protected override string CreateValue() => Guid.NewGuid().ToString();

       /* [Fact]
        public new void CorrectlyAdvancesReferenceCounter() => base.CorrectlyAdvancesReferenceCounter();*/
    }

    public class Tester
    {
        [Fact]
        public void DoTest()
        {
        }
    }
}
