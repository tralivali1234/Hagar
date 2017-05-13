using System;
using System.Binary;
using System.IO.Pipelines;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new PipeFactory();
            var pipe = factory.Create();

            var buffer = pipe.Writer.Alloc();
            new Writer().Write(buffer, 78);
            var res = new Writer().Read(pipe.Reader);
            Console.WriteLine(res);
            Console.WriteLine("Hello World!");
        }
        public class Writer
        {
            public void Write(WritableBuffer buffer, int i)
            {
                buffer.Ensure(4);
                buffer.Buffer.Span.Write(i);
                buffer.Advance(4);
                buffer.Commit();
            }
            public int Read(IPipeReader reader)
            {
                if (reader.TryRead(out var result))
                {
                    var value = result.Buffer.First.Span.Read<int>();
                    return value;
                }

                return -1;
            }
        }
    }
}