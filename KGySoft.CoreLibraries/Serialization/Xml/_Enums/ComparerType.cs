#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ComparerType.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.Serialization.Xml
{
    internal enum ComparerType
    {
        /// <summary>
        /// Represents an unknown or unexpected comparer for a known collection
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// Represents no comparer to (re)store or a null comparer.
        /// </summary>
        None = 0,

        /// <summary>
        /// Represents the default comparer type. Its actual type depends on the collection type:
        /// - Generic collection with IEqualityComparer`1: EqualityComparer`1.Default
        /// - Generic collection with IComparer`1: Comparer`1.Default
        /// - Non-generic collection with IComparer: Comparer.Default (with current culture)
        /// </summary>
        Default,

        DefaultInvariant, // Comparer.DefaultInvariant
        CaseInsensitive, // CaseInsensitiveComparer.Default or HybridDictionary case insensitivity
        CaseInsensitiveInvariant, // CaseInsensitiveComparer.DefaultInvariant
        Ordinal, // StringComparer.Ordinal
        OrdinalIgnoreCase, // StringComparer.OrdinalIgnoreCase
        Invariant, // StringComparer.InvariantCulture
        InvariantIgnoreCase, // StringComparer.InvariantCultureIgnoreCase
#if NET9_0_OR_GREATER // TODO - https://github.com/dotnet/runtime/issues/77679
#error check if already available
        OrdinalNonRandomized, // StringComparer.OrdinalNonRandomized
        OrdinalIgnoreCaseNonRandomized, // StringComparer.OrdinalIgnoreCaseNonRandomized  
#endif
        StringSegmentOrdinal, // StringSegmentOrdinal.Ordinal
        StringSegmentOrdinalIgnoreCase, // StringSegmentOrdinal.OrdinalIgnoreCase
        StringSegmentInvariant, // StringSegmentOrdinal.InvariantCulture
        StringSegmentInvariantIgnoreCase, // StringSegmentOrdinal.InvariantCultureIgnoreCase
        StringSegmentOrdinalRandomized, // StringSegmentOrdinal.OrdinalRandomized
        StringSegmentOrdinalIgnoreCaseRandomized, // StringSegmentOrdinal.OrdinalIgnoreCaseRandomized
        StringSegmentOrdinalNonRandomized, // StringSegmentOrdinal.OrdinalNonRandomized
        StringSegmentOrdinalIgnoreCaseNonRandomized, // StringSegmentOrdinal.OrdinalIgnoreCaseNonRandomized
        EnumComparer,
    }
}