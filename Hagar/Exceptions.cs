using System;
using System.Collections.Generic;

namespace Hagar
{
    public static class ExceptionHelper
    {
        public static T ThrowArgumentOutOfRange<T>(string argument) => throw new ArgumentOutOfRangeException(argument);
        public static void ThrowArgumentOutOfRange(string argument) => throw new ArgumentOutOfRangeException(argument);
    }

    public class HagarException : Exception
    {
        public HagarException()
        {
        }

        public HagarException(string message) : base(message)
        {
        }

        public HagarException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class FieldIdNotPresentException : HagarException
    {
        public FieldIdNotPresentException() : base("Attempted to access the field id from a tag which cannot have a field id.")
        {
        }
    }

    public class SchemaTypeInvalidException : HagarException
    {
        public SchemaTypeInvalidException() : base("Attempted to access the schema type from a tag which cannot have a schema type.")
        {
        }
    }

    public class FieldTypeInvalidException : HagarException
    {
        public FieldTypeInvalidException() : base("Attempted to access the schema type from a tag which cannot have a schema type.")
        {
        }
    }

    public class FieldTypeMissingException : HagarException
    {
        public FieldTypeMissingException(Type type) : base($"Attempted to deserialize an instance of abstract type {type}. No concrete type was provided.")
        {
        }
    }

    public class ExtendedWireTypeInvalidException : HagarException
    {
        public ExtendedWireTypeInvalidException() : base(
            "Attempted to access the extended wire type from a tag which does not have an extended wire type.")
        {
        }
    }

    public class UnsupportedWireTypeException : HagarException
    {
        public UnsupportedWireTypeException()
        {
        }

        public UnsupportedWireTypeException(string message) : base(message)
        {
        }
    }

    public class ReferenceNotFoundException : HagarException
    {
        public IDictionary<uint, object> References { get; }
        public uint TargetReference { get; }
        public Type TargetReferenceType { get; }

        public ReferenceNotFoundException(Type targetType, uint targetId, IDictionary<uint, object> references) : base(
            $"Reference with id {targetId} and type {targetType} not found. See {nameof(References)} property for existing references.")
        {
            this.TargetReference = targetId;
            this.TargetReferenceType = targetType;
            this.References = references;
        }

        // TODO: Add serialization members
    }
}