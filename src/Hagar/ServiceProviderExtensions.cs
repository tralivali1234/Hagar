﻿using System;
using System.Reflection;
using Hagar.Activators;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Configuration;
using Hagar.Serializers;
using Hagar.Session;
using Hagar.TypeSystem;
using Hagar.WireProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hagar
{
    public static class ServiceProviderExtensions
    {
        public static IServiceCollection AddHagar(this IServiceCollection services, Action<SerializerConfiguration> configure = null)
        {
            // Only add the services once.
            var existingContext = GetFromServices<HagarConfigurationContext>(services);
            if (existingContext == null)
            {
                services.Add(HagarConfigurationContext.Descriptor);

                services.AddSingleton<IConfigurationProvider<SerializerConfiguration>, DefaultSerializerConfiguration>();
                services.AddSingleton<IConfigurationProvider<TypeConfiguration>, DefaultTypeConfiguration>();
                services.TryAddSingleton(typeof(IActivator<>), typeof(DefaultActivator<>));
                services.TryAddSingleton(typeof(ListActivator<>));
                services.TryAddSingleton(typeof(DictionaryActivator<,>));
                services.TryAddSingleton(typeof(IConfiguration<>), typeof(ConfigurationHolder<>));
                services.TryAddSingleton<ITypeResolver, CachedTypeResolver>();
                services.TryAddSingleton<CodecProvider>();
                services.TryAddSingleton<IUntypedCodecProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<ITypedCodecProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<IPartialSerializerProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<IValueSerializerProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddScoped(typeof(IFieldCodec<>), typeof(FieldCodecHolder<>));
                services.TryAddScoped(typeof(IPartialSerializer<>), typeof(PartialSerializerHolder<>));
                services.TryAddScoped(typeof(IValueSerializer<>), typeof(ValueSerializerHolder<>));
                services.TryAddSingleton<WellKnownTypeCollection>();
                services.TryAddSingleton<TypeCodec>();

                // Session
                services.AddSingleton<SessionPool>();

                // Serializer
                services.AddSingleton(typeof(Serializer<>));
                services.AddSingleton(typeof(Serializer));
            }

            if (configure != null)
            {
                services.AddSingleton<IConfigurationProvider<SerializerConfiguration>>(
                    new DelegateConfigurationProvider<SerializerConfiguration>(configure));
            }

            return services;
        }

        private static T GetFromServices<T>(IServiceCollection services)
        {
            foreach (var service in services )
            {
                if (service.ServiceType == typeof(T)) return (T)service.ImplementationInstance;
            }

            return default;
        }

        private class HagarConfigurationContext
        {
            public static readonly ServiceDescriptor Descriptor = new ServiceDescriptor(typeof(HagarConfigurationContext), new HagarConfigurationContext());
        }

        public static IServiceCollection AddSerializers(this IServiceCollection services, Assembly asm)
        {
            var attrs = asm.GetCustomAttributes<MetadataProviderAttribute>();
            foreach (var attr in attrs)
            {
                if (!typeof(IConfigurationProvider<SerializerConfiguration>).IsAssignableFrom(attr.ProviderType)) continue;
                services.AddSingleton(typeof(IConfigurationProvider<SerializerConfiguration>), attr.ProviderType);
            }

            return services;
        }

        private class DelegateConfigurationProvider<TOptions> : IConfigurationProvider<TOptions>
        {
            private readonly Action<TOptions> configure;

            public DelegateConfigurationProvider(Action<TOptions> configure)
            {
                this.configure = configure;
            }

            public void Configure(TOptions configuration) => this.configure(configuration);
        }

        private class FieldCodecHolder<TField> : IFieldCodec<TField>, IServiceHolder<IFieldCodec<TField>>
        {
            private readonly ITypedCodecProvider codecProvider;
            private IFieldCodec<TField> codec;

            public FieldCodecHolder(ITypedCodecProvider codecProvider)
            {
                this.codecProvider = codecProvider;
            }
            
            public void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, TField value) => this.Value.WriteField(ref writer, session, fieldIdDelta, expectedType, value);
            
            public TField ReadValue(ref Reader reader, SerializerSession session, Field field) => this.Value.ReadValue(ref reader, session, field);

            public IFieldCodec<TField> Value => this.codec ?? (this.codec = this.codecProvider.GetCodec<TField>());
        }

        private class PartialSerializerHolder<TField> : IPartialSerializer<TField>, IServiceHolder<IPartialSerializer<TField>> where TField : class
        {
            private readonly IPartialSerializerProvider provider;
            private IPartialSerializer<TField> partialSerializer;

            public PartialSerializerHolder(IPartialSerializerProvider provider)
            {
                this.provider = provider;
            }

            public void Serialize(ref Writer writer, SerializerSession session, TField value)
            {
                this.Value.Serialize(ref writer, session, value);
            }

            public void Deserialize(ref Reader reader, SerializerSession session, TField value)
            {
                this.Value.Deserialize(ref reader, session, value);
            }

            public IPartialSerializer<TField> Value => this.partialSerializer ?? (this.partialSerializer = this.provider.GetPartialSerializer<TField>());
        }

        private class ValueSerializerHolder<TField> : IValueSerializer<TField>, IServiceHolder<IValueSerializer<TField>> where TField : struct
        {
            private readonly IValueSerializerProvider provider;
            private IValueSerializer<TField> serializer;

            public ValueSerializerHolder(IValueSerializerProvider provider)
            {
                this.provider = provider;
            }

            public void Serialize(ref Writer writer, SerializerSession session, ref TField value)
            {
                this.Value.Serialize(ref writer, session, ref value);
            }

            public void Deserialize(ref Reader reader, SerializerSession session, ref TField value)
            {
                this.Value.Deserialize(ref reader, session, ref value);
            }

            public IValueSerializer<TField> Value => this.serializer ?? (this.serializer = this.provider.GetValueSerializer<TField>());
        }
    }
}
