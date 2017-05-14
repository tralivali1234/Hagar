namespace Hagar
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    namespace Orleans.Serialization
    {
        public class Writer
        {
            private readonly ByteArrayBuilder ab;

            /// <summary> Default constructor. </summary>
            public Writer()
            {
                ab = new ByteArrayBuilder();
                Trace("Starting new binary token stream");
            }

            /// <summary> Return the output stream as a set of <c>ArraySegment</c>. </summary>
            /// <returns>Data from this stream, converted to output type.</returns>
            public List<ArraySegment<byte>> ToBytes()
            {
                return ab.ToBytes();
            }

            /// <summary> Return the output stream as a <c>byte[]</c>. </summary>
            /// <returns>Data from this stream, converted to output type.</returns>
            public byte[] ToByteArray()
            {
                return ab.ToByteArray();
            }

            /// <summary> Release any serialization buffers being used by this stream. </summary>
            public void ReleaseBuffers()
            {
                ab.ReleaseBuffers();
            }

            /// <summary> Current write position in the stream. </summary>
            public int CurrentOffset { get { return ab.Length; } }
            
            /// <summary> Write an <c>Int32</c> value to the stream. </summary>
            public void Write(int i)
            {
                Trace("--Wrote integer {0}", i);
                ab.Append(i);
            }

            /// <summary> Write an <c>Int16</c> value to the stream. </summary>
            public void Write(short s)
            {
                Trace("--Wrote short {0}", s);
                ab.Append(s);
            }

            /// <summary> Write an <c>Int64</c> value to the stream. </summary>
            public void Write(long l)
            {
                Trace("--Wrote long {0}", l);
                ab.Append(l);
            }

            /// <summary> Write a <c>sbyte</c> value to the stream. </summary>
            public void Write(sbyte b)
            {
                Trace("--Wrote sbyte {0}", b);
                ab.Append(b);
            }

            /// <summary> Write a <c>UInt32</c> value to the stream. </summary>
            public void Write(uint u)
            {
                Trace("--Wrote uint {0}", u);
                ab.Append(u);
            }

            /// <summary> Write a <c>UInt16</c> value to the stream. </summary>
            public void Write(ushort u)
            {
                Trace("--Wrote ushort {0}", u);
                ab.Append(u);
            }

            /// <summary> Write a <c>UInt64</c> value to the stream. </summary>
            public void Write(ulong u)
            {
                Trace("--Wrote ulong {0}", u);
                ab.Append(u);
            }

            /// <summary> Write a <c>byte</c> value to the stream. </summary>
            public void Write(byte b)
            {
                Trace("--Wrote byte {0}", b);
                ab.Append(b);
            }
            
            public void Write(Span<byte> span)
            {
                Trace("--Wrote span of length {0}", span.Length);
                ab.Append(span);
            }

            /// <summary> Write a <c>float</c> value to the stream. </summary>
            public void Write(float f)
            {
                Trace("--Wrote float {0}", f);
                ab.Append(f);
            }

            /// <summary> Write a <c>double</c> value to the stream. </summary>
            public void Write(double d)
            {
                Trace("--Wrote double {0}", d);
                ab.Append(d);
            }

            /// <summary> Write a <c>decimal</c> value to the stream. </summary>
            public void Write(decimal d)
            {
                Trace("--Wrote decimal {0}", d);
                ab.Append(Decimal.GetBits(d));
            }

            // Text

            /// <summary> Write a <c>string</c> value to the stream. </summary>
            public void Write(string s)
            {
                Trace("--Wrote string '{0}'", s);
                if (null == s)
                {
                    ab.Append(-1);
                }
                else
                {
                    var bytes = Encoding.UTF8.GetBytes(s);
                    ab.Append(bytes.Length);
                    ab.Append(bytes);
                }
            }

            /// <summary> Write a <c>char</c> value to the stream. </summary>
            public void Write(char c)
            {
                Trace("--Wrote char {0}", c);
                ab.Append(Convert.ToInt16(c));
            }
            
            // Primitive arrays

            /// <summary> Write a <c>byte[]</c> value to the stream. </summary>
            public void Write(byte[] b)
            {
                Trace("--Wrote byte array of length {0}", b.Length);
                ab.Append(b);
            }

            /// <summary> Write a list of byte array segments to the stream. </summary>
            public void Write(List<ArraySegment<byte>> bytes)
            {
                ab.Append(bytes);
            }

            /// <summary> Write the specified number of bytes to the stream, starting at the specified offset in the input <c>byte[]</c>. </summary>
            /// <param name="b">The input data to be written.</param>
            /// <param name="offset">The offset into the inout byte[] to start writing bytes from.</param>
            /// <param name="count">The number of bytes to be written.</param>
            public void Write(byte[] b, int offset, int count)
            {
                if (count <= 0)
                {
                    return;
                }
                Trace("--Wrote byte array of length {0}", count);
                if ((offset == 0) && (count == b.Length))
                {
                    Write(b);
                }
                else
                {
                    var temp = new byte[count];
                    Buffer.BlockCopy(b, offset, temp, 0, count);
                    Write(temp);
                }
            }

            /// <summary> Write a <c>Int16[]</c> value to the stream. </summary>
            public void Write(short[] i)
            {
                Trace("--Wrote short array of length {0}", i.Length);
                ab.Append(i);
            }

            /// <summary> Write a <c>Int32[]</c> value to the stream. </summary>
            public void Write(int[] i)
            {
                Trace("--Wrote short array of length {0}", i.Length);
                ab.Append(i);
            }

            /// <summary> Write a <c>Int64[]</c> value to the stream. </summary>
            public void Write(long[] l)
            {
                Trace("--Wrote long array of length {0}", l.Length);
                ab.Append(l);
            }

            /// <summary> Write a <c>UInt16[]</c> value to the stream. </summary>
            public void Write(ushort[] i)
            {
                Trace("--Wrote ushort array of length {0}", i.Length);
                ab.Append(i);
            }

            /// <summary> Write a <c>UInt32[]</c> value to the stream. </summary>
            public void Write(uint[] i)
            {
                Trace("--Wrote uint array of length {0}", i.Length);
                ab.Append(i);
            }

            /// <summary> Write a <c>UInt64[]</c> value to the stream. </summary>
            public void Write(ulong[] l)
            {
                Trace("--Wrote ulong array of length {0}", l.Length);
                ab.Append(l);
            }

            /// <summary> Write a <c>sbyte[]</c> value to the stream. </summary>
            public void Write(sbyte[] l)
            {
                Trace("--Wrote sbyte array of length {0}", l.Length);
                ab.Append(l);
            }

            /// <summary> Write a <c>char[]</c> value to the stream. </summary>
            public void Write(char[] l)
            {
                Trace("--Wrote char array of length {0}", l.Length);
                ab.Append(l);
            }

            /// <summary> Write a <c>bool[]</c> value to the stream. </summary>
            public void Write(bool[] l)
            {
                Trace("--Wrote bool array of length {0}", l.Length);
                ab.Append(l);
            }

            /// <summary> Write a <c>double[]</c> value to the stream. </summary>
            public void Write(double[] d)
            {
                Trace("--Wrote double array of length {0}", d.Length);
                ab.Append(d);
            }

            /// <summary> Write a <c>float[]</c> value to the stream. </summary>
            public void Write(float[] f)
            {
                Trace("--Wrote float array of length {0}", f.Length);
                ab.Append(f);
            }

            /// <summary> Write a <c>TimeSpan</c> value to the stream. </summary>
            public void Write(TimeSpan ts)
            {
                Write(ts.Ticks);
            }

            /// <summary> Write a <c>DataTime</c> value to the stream. </summary>
            public void Write(DateTime dt)
            {
                Write(dt.ToBinary());
            }

            /// <summary> Write a <c>Guid</c> value to the stream. </summary>
            public void Write(Guid id)
            {
                Write(id.ToByteArray());
            }
            

            private StreamWriter trace;

            [Conditional("TRACE_SERIALIZATION")]
            private void Trace(string format, params object[] args)
            {
                if (trace == null)
                {
                    var path = String.Format("d:\\Trace-{0}.{1}.{2}.txt", DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Ticks);
                    Console.WriteLine("Opening trace file at '{0}'", path);
                    trace = File.CreateText(path);
                }
                trace.Write(format, args);
                trace.WriteLine(" at offset {0}", CurrentOffset);
                trace.Flush();
            }
        }
    }
}
