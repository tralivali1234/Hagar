using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hagar.Activator;
using Hagar.Buffers;
using Hagar.Codec;
using Hagar.Metadata;
using Hagar.Serializer;
using Hagar.Session;
using Hagar.TypeSystem;
using Hagar.WireProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hagar
{
    public static class ServiceProviderExtensions
    {
        public static IServiceCollection AddCryoBuf(this IServiceCollection services)
        {
            services.AddSingleton<IMetadataProvider<CodecMetadata>, DefaultCodecProvider>();
            services.TryAddSingleton(typeof(IActivator<>), typeof(DefaultActivator<>));
            services.TryAddSingleton(typeof(IMetadata<>), typeof(MetadataHolder<>));
            services.TryAddSingleton<ITypeFilter, DefaultTypeFilter>();
            services.TryAddSingleton<CodecProvider>();
            services.TryAddSingleton<IUntypedCodecProvider>(sp => sp.GetRequiredService<CodecProvider>());
            services.TryAddSingleton<ITypedCodecProvider>(sp => sp.GetRequiredService<CodecProvider>());
            services.TryAddSingleton<IPartialSerializerProvider>(sp => sp.GetRequiredService<CodecProvider>());
            services.TryAddScoped(typeof(IFieldCodec<>), typeof(FieldCodecHolder<>));
            services.TryAddScoped(typeof(IPartialSerializer<>), typeof(PartialSerializerHolder<>));
            return services;
        }

        public static IServiceCollection AddSerializers(this IServiceCollection services, Assembly asm)
        {
            var attrs = asm.GetCustomAttributes<MetadataProviderAttribute>();
            foreach (var attr in attrs)
            {
                if (!typeof(IMetadataProvider<CodecMetadata>).IsAssignableFrom(attr.ProviderType)) continue;
                services.AddSingleton(typeof(IMetadataProvider<CodecMetadata>), attr.ProviderType);
            }

            return services;
        }

        private class FieldCodecHolder<TField> : IFieldCodec<TField>
        {
            private readonly IFieldCodec<TField> codec;

            public FieldCodecHolder(ITypedCodecProvider codecProvider)
            {
                this.codec = codecProvider.GetCodec<TField>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, TField value) => this.codec.WriteField(writer, session, fieldIdDelta, expectedType, value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TField ReadValue(Reader reader, SerializerSession session, Field field) => this.codec.ReadValue(reader, session, field);
        }

        private class PartialSerializerHolder<TField> : IPartialSerializer<TField> where TField : class
        {
            private readonly IPartialSerializer<TField> partialSerializer;
            public PartialSerializerHolder(IPartialSerializerProvider provider)
            {
                this.partialSerializer = provider.GetPartialSerializer<TField>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Serialize(Writer writer, SerializerSession session, TField value)
            {
                this.partialSerializer.Serialize(writer, session, value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Deserialize(Reader reader, SerializerSession session, TField value)
            {
                this.partialSerializer.Deserialize(reader, session, value);
            }
        }
    }
}
