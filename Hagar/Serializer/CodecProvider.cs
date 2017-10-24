using System;
using System.Collections.Generic;
using System.Reflection;
using Hagar.Codec;
using Hagar.Utilities;

namespace Hagar.Serializer
{
    public class CodecProvider : ITypedCodecProvider, IUntypedCodecProvider
    {
        private static readonly Type ObjectType = typeof(object);
        private static readonly Type OpenGenericCodecType = typeof(IFieldCodec<>);
        private static readonly MethodInfo TypedCodecWrapperCreateMethod = typeof(CodecWrapper).GetMethod(nameof(CodecWrapper.CreateUntypedFromTyped), BindingFlags.Public | BindingFlags.Static);

        private readonly CachedReadConcurrentDictionary<Type, IFieldCodec> adaptedCodecs = new CachedReadConcurrentDictionary<Type, IFieldCodec>();
        private readonly Dictionary<Type, IFieldCodec> codecInstances;
        private readonly List<IMultiCodec> multiCodecs;
        private readonly VoidCodec voidCodec = new VoidCodec();

#warning consider replacing with DI container.
        public CodecProvider(Dictionary<Type, IFieldCodec> codecInstances, List<IMultiCodec> multiCodecs)
        {
            this.codecInstances = codecInstances;
            this.multiCodecs = multiCodecs;
        }

        public IFieldCodec<TField> TryGetCodec<TField>() => this.TryGetCodec<TField>(typeof(TField));

        public IFieldCodec<object> GetCodec(Type fieldType) => this.TryGetCodec(fieldType) ?? ThrowCodecNotFound<object>(fieldType);

        public IFieldCodec<object> TryGetCodec(Type fieldType) => this.TryGetCodec<object>(fieldType);

        public IFieldCodec<TField> GetCodec<TField>() => this.TryGetCodec<TField>() ?? ThrowCodecNotFound<TField>(typeof(TField));

        private IFieldCodec<TField> TryGetCodec<TField>(Type fieldType)
        {
            // Try to find the codec from the configured codecs.
            IFieldCodec untypedResult;
            // TODO: Document why voidCodec is useful.
            if (fieldType == null) untypedResult = this.voidCodec;
            else if (!this.codecInstances.TryGetValue(fieldType, out untypedResult))
            {
                foreach (var dynamicCodec in this.multiCodecs)
                {
                    if (dynamicCodec.IsSupportedType(fieldType))
                    {
                        untypedResult = dynamicCodec;
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
                    typedResult = CodecWrapper.CreatedTypedFromUntyped<TField>(objectCodec);
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