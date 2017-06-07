using System;


namespace Hagar.Exceptions
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

    public class ExtendedWireTypeInvalidException : HagarException
    {
        public ExtendedWireTypeInvalidException() : base(
            "Attempted to access the extended wire type from a tag which does not have an extended wire type.")
        {
        }
    }
}