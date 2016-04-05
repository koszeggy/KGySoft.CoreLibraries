#region Used namespaces

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using KGySoft.Libraries.Reflection;

#endregion

namespace KGySoft.Libraries
{
    /// <summary>
    /// Formatting options for <see cref="Enum{TEnum}.ToString(TEnum,KGySoft.Libraries.EnumFormattingOptions,string)"/> method.
    /// </summary>
    public enum EnumFormattingOptions
    {
        /// <summary>
        /// Provides similar formatting as <see cref="Enum.ToString()"/>, though result is not guaranteed to be exactly the same if there are more defined names for the
        /// same value. Produced result will be always parseable by <see cref="Enum.Parse(System.Type,string)"/> as long as used separator is the comma character.
        /// </summary>
        Auto,

        /// <summary>
        /// Enum value is forced to be treated as non-flags value. If there is no defined name for the current value, then a number is returned.
        /// This result is always parseable by <see cref="Enum.Parse(System.Type,string)"/> as long as used separator is the comma character.
        /// </summary>
        NonFlags,

        /// <summary>
        /// Result will only contain those names, which are powers of number 2 (single bits). Missing names will be substituted by integers. Result
        /// will not be parseable by <see cref="Enum.Parse(System.Type,string)"/> if contains a number while at least two flags are set. To parse such a
        /// result <see cref="Enum{TEnum}.Parse(string)"/> can be used.
        /// </summary>
        DistinctFlags,

        /// <summary>
        /// Result can contain either defined names (including compound ones, which do not represent single bits) or a single numeric value. This behavior is similar to <see cref="Enum.ToString()"/>
        /// in case of a flags enumeration and result is always parseable by <see cref="Enum.Parse(System.Type,string)"/> as long as used separator is the comma character.
        /// </summary>
        CompoundFlagsOrNumber,

        /// <summary>
        /// Result can contain defined names (including compound ones, which do not represent single bits) and optionally also a numeric value if result cannot be covered by names.
        /// Result will not be parseable by <see cref="Enum.Parse(System.Type,string)"/> if contains a number along with names. To parse such a
        /// result <see cref="Enum{TEnum}.Parse(string)"/> can be used.
        /// </summary>
        CompoundFlagsAndNumber,
    }
}
