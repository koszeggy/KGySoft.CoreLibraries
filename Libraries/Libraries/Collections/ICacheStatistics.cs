namespace KGySoft.Libraries.Collections
{
    /// <summary>
    /// Represents cache statistics retrieved by <see cref="Cache{TKey,TValue}.GetStatistics"/>.
    /// </summary>
    /// <seealso cref="Cache{TKey,TValue}"/>
    public interface ICacheStatistics
    {
        #region Properties

        /// <summary>
        /// Gets number of cache reads.
        /// </summary>
        int Reads { get; }

        /// <summary>
        /// Gets number of cache writes.
        /// </summary>
        int Writes { get; }

        /// <summary>
        /// Gets number of cache deletes.
        /// </summary>
        int Deletes { get; }

        /// <summary>
        /// Gets number of cache hits.
        /// </summary>
        int Hits { get; }

        /// <summary>
        /// Gets the hit rate of the cache
        /// </summary>
        float HitRate { get; }

        #endregion
    }
}
