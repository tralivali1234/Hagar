using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Hagar;
using Hagar.Codecs;
using Microsoft.Build.Utilities;

namespace MyPocos
{
    [GenerateSerializer]
    public class SomeClassWithSerialzers
    {
        [Id(0)]
        public int IntProperty { get; set; }

        [Id(1)] public int IntField;

        public int UnmarkedField;

        public int UnmarkedProperty { get; set; }

        public override string ToString()
        {
            return $"{nameof(this.IntField)}: {this.IntField}, {nameof(this.IntProperty)}: {this.IntProperty}";
        }
    }

    [GenerateSerializer]
    public class SerializableClassWithCompiledBase : List<int>
    {
        [Id(0)]
        public int IntProperty { get; set; }
    }

    [GenerateSerializer]
    public class GenericPoco<T>
    {
        [Id(0)]
        public T Field { get; set; }

        [Id(1030)]
        public T[] ArrayField { get; set; }

        [Id(2222)]
        public Dictionary<T, T> DictField { get; set; }
    }

    [GenerateSerializer]
    public class GenericPocoWithConstraint<TClass, TStruct>
        : GenericPoco<TStruct> where TClass : List<int>, new() where TStruct : struct
    {
        [Id(0)]
        public new TClass Field { get; set; }

        [Id(999)]
        public TStruct ValueField { get; set; }
    }

    public interface IMyRemoteType
    {
        [Id(1)]
        Task<int> Echo(string s);
    }

    public struct ObjectId { }

    public struct Buffers { }

    internal interface IRuntime
    {
        (uint, Buffers) AllocateMessage();
        ValueTask<Buffers> InvokeRequest(uint id, Buffers buffers);
    }

    internal class Proxy : IMyRemoteType
    {
        private ObjectId o;
        private IRuntime runtime;
        private readonly IFieldCodec<int> intCodec;
        private readonly IFieldCodec<string> stringCodec;

        public Proxy(ObjectId o, IRuntime runtime, IFieldCodec<int> intCodec, IFieldCodec<string> stringCodec)
        {
            this.o = o;
            this.runtime = runtime;
            this.intCodec = intCodec;
            this.stringCodec = stringCodec;
        }

        public async Task<int> Echo(string s)
        {
            // Get message id (thread local, allocate in chunks)
            // Get buffers
            var (id, buffers) = runtime.AllocateMessage();

            // Serialize header (delegate to another method in this class or in runtime)
            //   - message id
            //   - message type (request)
            //   - target type (type of this interface)
            //   - target id

            // Serialize body
            // - method id
            // - arguments

            // Send message, awaiting result
            // -- NOTE: there should be no locals/params captured by the async state machine.
            // -- NOTE: exceptions are thrown from here. That includes network errors as well
            //    as errors thrown by remote user code.
            await runtime.InvokeRequest(id, buffers);

            // Deserialize payload into return type & return result
            return 0;// this.intCodec.ReadValue(null, null);
        }
    }
}
