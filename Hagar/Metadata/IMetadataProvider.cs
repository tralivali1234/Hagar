namespace Hagar.Metadata
{
    /// <summary>
    /// Provides metadata of the specified type.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    // ReSharper disable once TypeParameterCanBeVariant
    public interface IMetadataProvider<TMetadata>
    {
        /// <summary>
        /// Populates the provided metadata object.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        void PopulateMetadata(TMetadata metadata);
    }
}
