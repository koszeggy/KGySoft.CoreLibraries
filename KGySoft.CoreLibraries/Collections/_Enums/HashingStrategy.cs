#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: HashingStrategy.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
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
        /// For <see cref="string">string</see> keys and sealed key types without an overloaded <see cref="Object.GetHashCode">GetHashCode</see>
        /// the <see cref="And"/> hashing strategy will be used, while for any other key types the <see cref="Modulo"/> hashing strategy will be used.
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