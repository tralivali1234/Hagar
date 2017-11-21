using System;
using Hagar.TestKit;
using Xunit;

namespace Hagar.UnitTests
{
    /*public class StringCodecTests : FieldCodecTester<string, StringCodec>
    {
        //protected override StringCodec CreateCodec() => new StringCodec();

        //protected override string CreateValue() => Guid.NewGuid().ToString();

       /* [Fact]
        public new void CorrectlyAdvancesReferenceCounter() => base.CorrectlyAdvancesReferenceCounter();#1#
    }*/

    public class Tester
    {
        [Theory]
        [InlineData(23)]
        public void DoTest(int i)
        {
            Assert.False(67 + 34 - i == 0);
        }
    }
}
