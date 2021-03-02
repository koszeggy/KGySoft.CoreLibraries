#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: HashingStrategy.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a hashing strategy for some hash-based dictionaries and caches.
    /// </summary>
    public enum HashingStrategy
    {
        /// <summary>
        /// The hashing strategy is determined by the type of the key in the storage.
        /// For <see cref="string">string</see> keys and sealed key types without an overloaded <see cref="Object.GetHashCode">GetHashCode</see> it will use
        /// the <see cref="And"/> hashing strategy, while for any other key types it will use the <see cref="Modulo"/> hashing strategy.
        /// </summary>
        Auto,

        /// <summary>
        /// Represents the modulo division hashing strategy. This is quite robust even for poor <see cref="Object.GetHashCode">GetHashCode</see> implementations
        /// but is a bit slower than the bitwise AND hashing strategy.
        /// </summary>
        Modulo,

        /// <summary>
        /// Represents the bitwise AND hashing strategy. While the hashing itself is very fast, this solution is quite sensitive for
        /// poorer <see cref="Object.GetHashCode">GetHashCode</see> implementations that may cause many key collisions, which may end up
        /// in a poorer performance.
        /// </summary>
        And
    }
}