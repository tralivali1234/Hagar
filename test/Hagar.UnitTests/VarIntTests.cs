using System.Linq;
using Hagar.Buffers;
using Hagar.TestKit;
using Hagar.Utilities;
using Xunit;

namespace Hagar.UnitTests
{
    [Trait("Category", "BVT")]
    public class VarIntTests
    {
        [Fact]
        void CanRoundTripVarInt32()
        {
            var buffer = new SingleSegmentBuffer();

            void Test(uint num, bool fastRead)
            {
                var writer = new Writer(buffer);
                writer.WriteVarInt(num);
                Assert.Equal(PrefixVarIntHelpers.CountRequiredBytes(num), writer.Position);
                if (fastRead) writer.Write(0); // Extend the written amount so that there is always enough data to perform a fast read
                writer.Commit();

                var reader = new Reader(buffer.GetReadOnlySequence());
                var read = reader.ReadVarUInt32();

                Assert.Equal(num, read);
                Assert.Equal(PrefixVarIntHelpers.CountRequiredBytes(num), (int) reader.Position);
                buffer.Reset();
            }

            var nums = new uint[] { 0, 1, 1 << 7, 1 << 8, 1 << 15, 1 << 16, 1 << 20, 1 << 21, 1 << 24, 1 << 31 + 1234, 1234, 0xefefefef, uint.MaxValue };
            foreach (var fastRead in new[] {false, true})
            {
                foreach (var num in nums.Concat(Enumerable.Range(1, 512).Select(n => (uint)n)))
                {
                    Test(num, fastRead);
                }
            }

            {
                var writer = new Writer(buffer);
                foreach (var num in nums)
                {
                    writer.WriteVarInt(num);
                }

                writer.Commit();

                var reader = new Reader(buffer.GetReadOnlySequence());
                foreach (var num in nums)
                {
                    var read = reader.ReadVarUInt32();

                    Assert.Equal(num, read);
                }

                Assert.Equal(writer.Position, reader.Position);

                buffer.Reset();
            }
        }
    }
}