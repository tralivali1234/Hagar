using System;
using System.Collections.Generic;
using System.Reflection;
using Hagar.Codec;
using Hagar.Utilities;

namespace Hagar.Serializer
{
    public interface IUntypedCodecProvider
    {
        IFieldCodec<object> GetCodec(Type fieldType);
        IFieldCodec<object> TryGetCodec(Type fieldType);
    }

    public interface ITypedCodecProvider : IUntypedCodecProvider
    {
        IFieldCodec<TField> GetCodec<TField>();
        IFieldCodec<TField> TryGetCodec<TField>();
    }

    public interface IWrappedCodec
    {
        object InnerCodec { get; }
    }

    public interface IObjectCodec : IFieldCodec<object>
    {
        bool IsSupportedType(Type type);
    }

    public class CodecProvider : ITypedCodecProvider
    {
        private static readonly Type ObjectType = typeof(object);
        private static readonly Type OpenGenericCodecType = typeof(IFieldCodec<>);
        private static readonly MethodInfo TypedCodecWrapperCreateMethod = typeof(TypedCodecWrapper).GetMethod(nameof(TypedCodecWrapper.Create), BindingFlags.Public | BindingFlags.Static);

        private readonly CachedReadConcurrentDictionary<Type, object> adaptedCodecs = new CachedReadConcurrentDictionary<Type, object>();
        private readonly Dictionary<Type, object> codecInstances;
        private readonly List<IObjectCodec> dynamicCodecs;
        private readonly VoidCodec voidCodec = new VoidCodec();

        public CodecProvider(Dictionary<Type, object> codecInstances, List<IObjectCodec> dynamicCodecs)
        {
            this.codecInstances = codecInstances;
            this.dynamicCodecs = dynamicCodecs;
        }

        public IFieldCodec<TField> TryGetCodec<TField>() => this.TryGetCodec<TField>(typeof(TField));

        public IFieldCodec<object> GetCodec(Type fieldType) => this.TryGetCodec(fieldType) ?? ThrowCodecNotFound<object>(fieldType);

        public IFieldCodec<object> TryGetCodec(Type fieldType) => this.TryGetCodec<object>(fieldType);

        public IFieldCodec<TField> GetCodec<TField>() => this.TryGetCodec<TField>() ?? ThrowCodecNotFound<TField>(typeof(TField));

        private IFieldCodec<TField> TryGetCodec<TField>(Type fieldType)
        {
            // Try to find the codec from the configured codecs.
            object untypedResult;
            // TODO: Document why voidCodec is useful.
            if (fieldType == null) untypedResult = this.voidCodec;
            else if (!this.codecInstances.TryGetValue(fieldType, out untypedResult))
            {
                foreach (var genericCodec in this.dynamicCodecs)
                {
                    if (genericCodec.IsSupportedType(fieldType))
                    {
                        untypedResult = genericCodec;
                        break;
                    }
                }
            }

            // Check if the result fits a strongly-typed codec signature.
            switch (untypedResult)
            {
                case null:
                    return null;
                case IFieldCodec<TField> typedCodec:
                    return typedCodec;
                case IWrappedCodec wrapped when wrapped.InnerCodec is IFieldCodec<TField> typedCodec:
                    return typedCodec;
            }

            // Check if a codec has already been adapted to the target type.
            if (this.adaptedCodecs.TryGetValue(fieldType, out var previouslyAdapted))
            {
                untypedResult = previouslyAdapted;
            }

            // Attempt to adapt the codec if it's not already adapted.
            IFieldCodec<TField> typedResult;
            switch (untypedResult)
            {
                case IFieldCodec<TField> typedCodec:
                    return typedCodec;
                case IFieldCodec<object> objectCodec:
                    typedResult = new UntypedCodecWrapper<TField>(objectCodec);
                    break;
                default:
                    typedResult = TryWrapCodec(untypedResult);
                    break;
            }

            // Store the results or throw if adaptation failed.
            if (typedResult != null)
            {
                untypedResult = typedResult;
                typedResult = (IFieldCodec<TField>) this.adaptedCodecs.GetOrAdd(fieldType, _ => untypedResult);
            }
            else
            {
                ThrowCannotConvert(untypedResult);
            }

            return typedResult;

            IFieldCodec<TField> TryWrapCodec(object rawCodec)
            {
                var codecType = rawCodec.GetType();
                if (typeof(TField) == ObjectType)
                {
                    foreach (var @interface in codecType.GetInterfaces())
                    {
                        if (@interface.IsConstructedGenericType
                            && OpenGenericCodecType.IsAssignableFrom(@interface.GetGenericTypeDefinition()))
                        {
                            // Convert the typed codec provider into a wrapped object codec provider.
                            return TypedCodecWrapperCreateMethod.MakeGenericMethod(@interface.GetGenericArguments()[0], codecType).Invoke(null, new[] { rawCodec }) as IFieldCodec<TField>;
                        }
                    }
                }

                return null;
            }

            void ThrowCannotConvert(object rawCodec)
            {
                throw new InvalidOperationException($"Cannot convert codec of type {rawCodec.GetType()} to codec of type {typeof(IFieldCodec<TField>)}.");
            }
        }

        // TODO: Use a library-specific exception.
        private static IFieldCodec<TField> ThrowCodecNotFound<TField>(Type fieldType) => throw new KeyNotFoundException($"Could not find a codec for type {fieldType}.");
    }
}