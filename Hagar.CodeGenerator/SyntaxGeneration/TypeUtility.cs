using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Hagar.CodeGenerator.SyntaxGeneration
{
    internal static class TypeUtility
    {
        private static readonly ConcurrentDictionary<Tuple<Type, TypeFormattingOptions>, string> ParseableNameCache = new ConcurrentDictionary<Tuple<Type, TypeFormattingOptions>, string>();

        /// <summary>
        /// Returns the non-generic type name without any special characters.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The non-generic type name without any special characters.
        /// </returns>
        public static string GetUnadornedTypeName(this Type type)
        {
            var index = type.Name.IndexOf('`');

            // An ampersand can appear as a suffix to a by-ref type.
            return (index > 0 ? type.Name.Substring(0, index) : type.Name).TrimEnd('&');
        }

        /// <summary>
        /// Returns the non-generic method name without any special characters.
        /// </summary>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// The non-generic method name without any special characters.
        /// </returns>
        public static string GetUnadornedMethodName(this MethodInfo method)
        {
            var index = method.Name.IndexOf('`');

            return index > 0 ? method.Name.Substring(0, index) : method.Name;
        }

        /// <summary>Returns a string representation of <paramref name="type"/>.</summary>
        /// <param name="type">The type.</param>
        /// <param name="options">The type formatting options.</param>
        /// <param name="getNameFunc">The delegate used to get the unadorned, simple type name of <paramref name="type"/>.</param>
        /// <returns>A string representation of the <paramref name="type"/>.</returns>
        public static string GetParseableName(this Type type, TypeFormattingOptions options = null, Func<Type, string> getNameFunc = null)
        {
            options = options ?? TypeFormattingOptions.Default;

            // If a naming function has been specified, skip the cache.
            if (getNameFunc != null) return BuildParseableName();

            return ParseableNameCache.GetOrAdd(Tuple.Create(type, options), _ => BuildParseableName());

            string BuildParseableName()
            {
                var builder = new StringBuilder();
                var typeInfo = type.GetTypeInfo();
                GetParseableName(
                    type,
                    builder,
                    new Queue<Type>(
                        typeInfo.IsGenericTypeDefinition
                            ? typeInfo.GetGenericArguments()
                            : typeInfo.GenericTypeArguments),
                    options,
                    getNameFunc ?? (t => t.GetUnadornedTypeName() + options.NameSuffix));
                return builder.ToString();
            }
        }

        private static void GetParseableName(
            Type type,
            StringBuilder builder,
            Queue<Type> typeArguments,
            TypeFormattingOptions options,
            Func<Type, string> getNameFunc)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsArray)
            {
                var elementType = typeInfo.GetElementType().GetParseableName(options);
                if (!string.IsNullOrWhiteSpace(elementType))
                {
                    builder.AppendFormat(
                        "{0}[{1}]",
                        elementType,
                        string.Concat(Enumerable.Range(0, type.GetArrayRank() - 1).Select(_ => ',')));
                }

                return;
            }

            if (typeInfo.IsGenericParameter)
            {
                if (options.IncludeGenericTypeParameters)
                {
                    builder.Append(type.GetUnadornedTypeName());
                }

                return;
            }

            if (typeInfo.DeclaringType != null)
            {
                // This is not the root type.
                GetParseableName(typeInfo.DeclaringType, builder, typeArguments, options, t => t.GetUnadornedTypeName());
                builder.Append(options.NestedTypeSeparator);
            }
            else if (!string.IsNullOrWhiteSpace(type.Namespace) && options.IncludeNamespace)
            {
                // This is the root type, so include the namespace.
                var namespaceName = type.Namespace;
                if (options.NestedTypeSeparator != '.')
                {
                    namespaceName = namespaceName.Replace('.', options.NestedTypeSeparator);
                }

                if (options.IncludeGlobal)
                {
                    builder.AppendFormat("global::");
                }

                builder.AppendFormat("{0}{1}", namespaceName, options.NestedTypeSeparator);
            }

            if (type.IsConstructedGenericType)
            {
                // Get the unadorned name, the generic parameters, and add them together.
                var unadornedTypeName = getNameFunc(type);
                builder.Append(Identifier.EscapeIdentifier(unadornedTypeName));
                var generics =
                    Enumerable.Range(0, Math.Min(typeInfo.GetGenericArguments().Count(), typeArguments.Count))
                              .Select(_ => typeArguments.Dequeue())
                              .ToList();
                if (generics.Count > 0 && options.IncludeTypeParameters)
                {
                    var genericParameters = string.Join(
                        ",",
                        generics.Select(generic => GetParseableName(generic, options)));
                    builder.AppendFormat("<{0}>", genericParameters);
                }
            }
            else if (typeInfo.IsGenericTypeDefinition)
            {
                // Get the unadorned name, the generic parameters, and add them together.
                var unadornedTypeName = getNameFunc(type);
                builder.Append(Identifier.EscapeIdentifier(unadornedTypeName));
                var generics =
                    Enumerable.Range(0, Math.Min(type.GetGenericArguments().Count(), typeArguments.Count))
                              .Select(_ => typeArguments.Dequeue())
                              .ToList();
                if (generics.Count > 0 && options.IncludeTypeParameters)
                {
                    var genericParameters = string.Join(
                        ",",
                        generics.Select(_ => options.IncludeGenericTypeParameters ? _.ToString() : string.Empty));
                    builder.AppendFormat("<{0}>", genericParameters);
                }
            }
            else
            {
                builder.Append(Identifier.EscapeIdentifier(getNameFunc(type)));
            }
        }

        /// <summary>
        /// Options for formatting type names.
        /// </summary>
        public class TypeFormattingOptions : IEquatable<TypeFormattingOptions>
        {
            /// <summary>Initializes a new instance of <see cref="TypeFormattingOptions"/>.</summary>
            public TypeFormattingOptions(
                string nameSuffix = null,
                bool includeNamespace = true,
                bool includeGenericParameters = true,
                bool includeTypeParameters = true,
                char nestedClassSeparator = '.',
                bool includeGlobal = true)
            {

                this.NameSuffix = nameSuffix;
                this.IncludeNamespace = includeNamespace;
                this.IncludeGenericTypeParameters = includeGenericParameters;
                this.IncludeTypeParameters = includeTypeParameters;
                this.NestedTypeSeparator = nestedClassSeparator;
                this.IncludeGlobal = includeGlobal;
            }

            internal static TypeFormattingOptions Default { get; } = new TypeFormattingOptions();
            internal static TypeFormattingOptions LogFormat { get; } = new TypeFormattingOptions(includeGlobal: false);

            /// <summary>
            /// Gets a value indicating whether or not to include the fully-qualified namespace of the class in the result.
            /// </summary>
            public bool IncludeNamespace { get; }

            /// <summary>
            /// Gets a value indicating whether or not to include concrete type parameters in the result.
            /// </summary>
            public bool IncludeTypeParameters { get; }

            /// <summary>
            /// Gets a value indicating whether or not to include generic type parameters in the result.
            /// </summary>
            public bool IncludeGenericTypeParameters { get; }

            /// <summary>
            /// Gets the separator used between declaring types and their declared types.
            /// </summary>
            public char NestedTypeSeparator { get; }

            /// <summary>
            /// Gets the name to append to the formatted name, before any type parameters.
            /// </summary>
            public string NameSuffix { get; }

            /// <summary>
            /// Gets a value indicating whether or not to include the global namespace qualifier.
            /// </summary>
            public bool IncludeGlobal { get; }

            /// <summary>
            /// Indicates whether the current object is equal to another object of the same type.
            /// </summary>
            /// <param name="other">An object to compare with this object.</param>
            /// <returns>
            /// <see langword="true"/> if the specified object  is equal to the current object; otherwise, <see langword="false"/>.
            /// </returns>
            public bool Equals(TypeFormattingOptions other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }
                if (ReferenceEquals(this, other))
                {
                    return true;
                }
                return this.IncludeNamespace == other.IncludeNamespace
                       && this.IncludeTypeParameters == other.IncludeTypeParameters
                       && this.IncludeGenericTypeParameters == other.IncludeGenericTypeParameters
                       && this.NestedTypeSeparator == other.NestedTypeSeparator
                       && string.Equals(this.NameSuffix, other.NameSuffix) && this.IncludeGlobal == other.IncludeGlobal;
            }

            /// <summary>
            /// Determines whether the specified object is equal to the current object.
            /// </summary>
            /// <param name="obj">The object to compare with the current object.</param>
            /// <returns>
            /// <see langword="true"/> if the specified object  is equal to the current object; otherwise, <see langword="false"/>.
            /// </returns>
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (obj.GetType() != this.GetType())
                {
                    return false;
                }
                return Equals((TypeFormattingOptions) obj);
            }

            /// <summary>
            /// Serves as a hash function for a particular type. 
            /// </summary>
            /// <returns>
            /// A hash code for the current object.
            /// </returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = this.IncludeNamespace.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.IncludeTypeParameters.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.IncludeGenericTypeParameters.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.NestedTypeSeparator.GetHashCode();
                    hashCode = (hashCode * 397) ^ (this.NameSuffix != null ? this.NameSuffix.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ this.IncludeGlobal.GetHashCode();
                    return hashCode;
                }
            }

            /// <summary>Determines whether the specified objects are equal.</summary>
            public static bool operator ==(TypeFormattingOptions left, TypeFormattingOptions right)
            {
                return Equals(left, right);
            }

            /// <summary>Determines whether the specified objects are not equal.</summary>
            public static bool operator !=(TypeFormattingOptions left, TypeFormattingOptions right)
            {
                return !Equals(left, right);
            }
        }
    }
}