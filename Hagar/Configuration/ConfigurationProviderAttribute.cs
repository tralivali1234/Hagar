using System;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Hagar.Configuration
{
    /// <summary>
    /// Defines a metadata provider for this assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ConfigurationProviderAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationProviderAttribute"/> class.
        /// </summary>
        /// <param name="providerType">The metadata provider type.</param>
        public ConfigurationProviderAttribute(Type providerType)
        {
            if (providerType == null) throw new ArgumentNullException(nameof(providerType));
            if (!providerType.GetInterfaces().Any(iface => iface.IsConstructedGenericType && typeof(IConfigureOptions<>).IsAssignableFrom(iface.GetGenericTypeDefinition())))
            {
                throw new ArgumentException($"Provided type {providerType} must implement {typeof(IConfigureOptions<>)}", nameof(providerType));
            }

            this.ProviderType = providerType;
        }

        /// <summary>
        /// Gets the metadata provider type.
        /// </summary>
        public Type ProviderType { get; }
    }
}