namespace Hagar.Metadata
{
    /// <summary>
    /// Holds metadata of the specified type.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    // ReSharper disable once TypeParameterCanBeVariant
    public interface IMetadata<TMetadata> where TMetadata : class, new()
    {
        /// <summary>
        /// Gets the metadata value.
        /// </summary>
        TMetadata Value { get; }
    }
}