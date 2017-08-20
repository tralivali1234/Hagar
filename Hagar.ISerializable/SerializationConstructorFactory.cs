using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace Hagar.ISerializable
{
    internal class SerializationConstructorFactory
    {
        private static readonly Type DelegateType = typeof(Func<SerializationInfo, StreamingContext, object>);
        private static readonly Type[] ParameterTypes = {typeof(SerializationInfo), typeof(StreamingContext)};

        public Func<SerializationInfo, StreamingContext, object> GetSerializationConstructor(Type type)
        {
            var ctor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] {typeof(SerializationInfo), typeof(StreamingContext)},
                null);
            if (ctor == null) return ThrowSerializationConstructorNotFound(type);

            var method = new DynamicMethod($"{type}_serialization_ctor", DelegateType, ParameterTypes, DelegateType);
            var il = method.GetILGenerator();

            il.DeclareLocal(type);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);

            return (Func<SerializationInfo, StreamingContext, object>) method.CreateDelegate(DelegateType);
        }

        private static Func<SerializationInfo, StreamingContext, object> ThrowSerializationConstructorNotFound(Type type) =>
            throw new SerializationConstructorNotFoundException(type);
    }

    [Serializable]
    public class SerializationConstructorNotFoundException : Exception
    {
        public SerializationConstructorNotFoundException(Type type) : base(
            $"Could not find a suitable serialization constructor on type {type.FullName}")
        {
        }

        protected SerializationConstructorNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}