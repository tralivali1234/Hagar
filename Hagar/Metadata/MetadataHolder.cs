using System.Collections.Generic;

namespace Hagar.Metadata
{
    /// <inheritdoc />
    internal class MetadataHolder<TMetadata> : IMetadata<TMetadata> where TMetadata : class, new()
    {
        /// <inheritdoc />
        public MetadataHolder(IEnumerable<IMetadataProvider<TMetadata>> providers)
        {
            this.Value = new TMetadata();
            foreach (var provider in providers)
            {
                provider.PopulateMetadata(this.Value);
            }
        }

        /// <inheritdoc />
        public TMetadata Value { get; }
    }
}