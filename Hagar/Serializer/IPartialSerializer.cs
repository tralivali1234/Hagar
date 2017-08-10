﻿using System.Diagnostics.CodeAnalysis;
using Hagar.Session;
using Hagar.Utilities;

namespace Hagar.Serializer
{
    /// <summary>
    /// Serializer the content of a specified type without framing the type itself.
    /// </summary>
    /// <typeparam name="T">The type which this implementation can serialize and deserialize.</typeparam>
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
    public interface IPartialSerializer<T> where T : class
    {
        void Serialize(Writer writer, SerializerSession session, T value);
        void Deserialize(Reader reader, SerializerSession session, T value);
    }
}