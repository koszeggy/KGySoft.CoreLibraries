#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICacheStatistics.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.Collections
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
