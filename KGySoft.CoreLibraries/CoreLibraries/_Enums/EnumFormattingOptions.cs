#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumFormattingOptions.cs
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

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Formatting options for the <see cref="Enum{TEnum}.ToString(TEnum,EnumFormattingOptions,string)"><![CDATA[Enum<TEnum>.ToString(TEnum, EnumFormattingOptions, string)]]></see> method.
    /// </summary>
    public enum EnumFormattingOptions
    {
        /// <summary>
        /// Provides a similar formatting to the <see cref="Enum.ToString()">Enum.ToString</see> method, though result is not guaranteed to be exactly the same if there are more defined names for the
        /// same value. The produced result will always be parseable by the <see cref="Enum.Parse(Type,string)">System.Enum.Parse(Type, string)</see> method as long as used separator is the comma character.
        /// </summary>
        Auto,

        /// <summary>
        /// The <see langword="enum"/>&#160;value is forced to be treated as a non-flags value. If there is no defined name for the current value, then a number is returned.
        /// This result is always parseable by the <see cref="Enum.Parse(Type,string)">System.Enum.Parse(Type, string)</see> method.
        /// </summary>
        NonFlags,

        /// <summary>
        /// The result will contain only names of single bit values. Missing names will be substituted by integers. Result
        /// will not be parseable by the <see cref="Enum.Parse(Type,string)">System.Enum.Parse(Type, string)</see> method if the string contains a non-standalone number. To parse such a
        /// result the <see cref="O:KGySoft.CoreLibraries.Enum`1.Parse"><![CDATA[Enum<TEnum>.Parse]]></see> and <see cref="O:KGySoft.CoreLibraries.Enum`1.TryParse"><![CDATA[Enum<TEnum>.TryParse]]></see> overloads can be used.
        /// </summary>
        DistinctFlags,

        /// <summary>
        /// The result can contain either defined names (including compound ones, which do not represent single bits) or a single numeric value. This behavior is similar to the <see cref="Enum.ToString()">Enum.ToString</see> method
        /// for a <see cref="FlagsAttribute">Flags</see>&#160;<see langword="enum"/>&#160;and the result is always parseable by the <see cref="Enum.Parse(Type,string)">System.Enum.Parse(Type, string)</see> method as long as the separator is the comma character.
        /// </summary>
        CompoundFlagsOrNumber,

        /// <summary>
        /// The result can contain defined names (including compound ones, which do not represent single bits) and optionally also a numeric value if the result cannot be covered only by names.
        /// The result will not be parseable by the <see cref="Enum.Parse(Type,string)">System.Enum.Parse(Type, string)</see> method if a number was applied to the names. To parse such a
        /// result the <see cref="O:KGySoft.CoreLibraries.Enum`1.Parse"><![CDATA[Enum<TEnum>.Parse]]></see> and <see cref="O:KGySoft.CoreLibraries.Enum`1.TryParse"><![CDATA[Enum<TEnum>.TryParse]]></see> overloads can be used.
        /// </summary>
        CompoundFlagsAndNumber,

        /// <summary>
        /// The result is always a number, even if the value or flags has a named alternative.
        /// </summary>
        Number,
    }
}
