#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumFormattingOptions.cs
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

#region Usings

using System;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Formatting options for the <see cref="Enum{TEnum}.ToString(TEnum,EnumFormattingOptions,string)"><![CDATA[Enum<TEnum>.ToString(TEnum, EnumFormattingOptions, string)]]></see> method.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1717:OnlyFlagsEnumsShouldHavePluralNames", Justification = "Would be a breaking change")]
    public enum EnumFormattingOptions
    {
        /// <summary>
        /// Provides similar formatting as the <see cref="Enum.ToString()">Enum.ToString</see> method, though result is not guaranteed to be exactly the same if there are more defined names for the
        /// same value. Produced result will be always parseable by the <see cref="Enum.Parse(Type,string)">Enum.Parse(Type, string)</see> method as long as used separator is the comma character.
        /// </summary>
        Auto,

        /// <summary>
        /// The <see langword="enum"/>&#160;value is forced to be treated as non-flags value. If there is no defined name for the current value, then a number is returned.
        /// This result is always parseable by the <see cref="Enum.Parse(Type,string)">Enum.Parse(Type, string)</see> method as long as used separator is the comma character.
        /// </summary>
        NonFlags,

        /// <summary>
        /// Result will only contain those names, which are powers of number 2 (single bits). Missing names will be substituted by integers. Result
        /// will not be parseable by the <see cref="Enum.Parse(Type,string)">Enum.Parse(Type, string)</see> method if contains a number while at least two flags are set. To parse such a
        /// result the <see cref="O:KGySoft.CoreLibraries.Enum`1.Parse"><![CDATA[Enum<TEnum>.Parse]]></see> and <see cref="O:KGySoft.CoreLibraries.Enum`1.TryParse"><![CDATA[Enum<TEnum>.TryParse]]></see> overloads can be used.
        /// </summary>
        DistinctFlags,

        /// <summary>
        /// Result can contain either defined names (including compound ones, which do not represent single bits) or a single numeric value. This behavior is similar to the <see cref="Enum.ToString()">Enum.ToString</see> method
        /// in case of a flags enumeration and the result is always parseable by the <see cref="Enum.Parse(Type,string)">Enum.Parse(Type, string)</see> method as long as used separator is the comma character.
        /// </summary>
        CompoundFlagsOrNumber,

        /// <summary>
        /// Result can contain defined names (including compound ones, which do not represent single bits) and optionally also a numeric value if result cannot be covered by names.
        /// Result will not be parseable by the <see cref="Enum.Parse(Type,string)">Enum.Parse(Type, string)</see> method if contains a number along with names. To parse such a
        /// result the <see cref="O:KGySoft.CoreLibraries.Enum`1.Parse"><![CDATA[Enum<TEnum>.Parse]]></see> and <see cref="O:KGySoft.CoreLibraries.Enum`1.TryParse"><![CDATA[Enum<TEnum>.TryParse]]></see> overloads can be used.
        /// </summary>
        CompoundFlagsAndNumber,
    }
}
