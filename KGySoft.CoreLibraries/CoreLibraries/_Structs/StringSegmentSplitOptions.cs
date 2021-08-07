#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegmentSplitOptions.cs
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Specifies options for applicable <see cref="O:KGySoft.CoreLibraries.StringSegment.Split">StringSegment.Split</see> method overloads,
    /// such as whether to omit empty substrings from the returned array or trim whitespace from segments.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="StringSegmentSplitOptions"/> is specified as a <see langword="struct"/>&#160;rather than an <see langword="enum"/>,
    /// so it can be compatible both with <see cref="StringSplitOptions"/> and the old <see cref="O:KGySoft.CoreLibraries.StringSegment.Split">StringSegment.Split</see>
    /// methods that defined a simple <see cref="bool">bool</see>&#160;<c>removeEmptyEntries</c> parameter as options.</para>
    /// <para>Unlike <see cref="StringSplitOptions"/>, this struct defines the <see cref="TrimEntries"/> option for all platform targets.</para>
    /// </remarks>
    [DebuggerDisplay("{" + nameof(DebugValue) + ",nq}")]
    public readonly struct StringSegmentSplitOptions : IEquatable<StringSegmentSplitOptions>
    {
        #region Enumerations

        [Flags]
        private enum Options
        {
            None = 0,
            RemoveEmptyEntries = 1,
            TrimEntries = 2
        }

        #endregion

        #region Fields

        #region Static Fields

        /// <summary>
        /// Represents the default options when splitting string segments by the <see cref="O:KGySoft.CoreLibraries.StringSegment.Split">Split</see> method overloads.
        /// </summary>
        public static readonly StringSegmentSplitOptions None = new StringSegmentSplitOptions(Options.None);

        /// <summary>
        /// Omits elements that contain an empty string segment from the result. When combined with the <see cref="TrimEntries"/> option,
        /// then whitespace-only segments will also be omitted.
        /// </summary>
        public static readonly StringSegmentSplitOptions RemoveEmptyEntries = new StringSegmentSplitOptions(Options.RemoveEmptyEntries);

        /// <summary>
        /// Trims white-space characters from each string segment in the result. When combined with the <see cref="RemoveEmptyEntries"/> option,
        /// then whitespace-only segments will also be omitted.
        /// </summary>
        public static readonly StringSegmentSplitOptions TrimEntries = new StringSegmentSplitOptions(Options.TrimEntries);

        #endregion

        #region Instance Fields

        private readonly Options value;

        #endregion

        #endregion

        #region Properties

        #region Internal Properties

        internal bool IsRemoveEmpty => (value & Options.RemoveEmptyEntries) != 0;

        internal bool IsTrim => (value & Options.TrimEntries) != 0;

        #endregion

        #region Private Properties

        private string DebugValue => Enum<Options>.ToString(value, " | ");

        #endregion

        #endregion

        #region Operators

        /// <summary>
        /// Determines whether two specified <see cref="StringSegmentSplitOptions"/> instances have the same value.
        /// </summary>
        /// <param name="left">The left argument of the equality check.</param>
        /// <param name="right">The right argument of the equality check.</param>
        /// <returns>The result of the equality check.</returns>
        public static bool operator ==(StringSegmentSplitOptions left, StringSegmentSplitOptions right) => left.Equals(right);

        /// <summary>
        /// Determines whether two specified <see cref="StringSegmentSplitOptions"/> instances have different values.
        /// </summary>
        /// <param name="left">The left argument of the equality check.</param>
        /// <param name="right">The right argument of the equality check.</param>
        /// <returns>The result of the inequality check.</returns>
        public static bool operator !=(StringSegmentSplitOptions left, StringSegmentSplitOptions right) => !(left == right);

        /// <summary>
        /// Performs an implicit conversion from <see cref="StringSplitOptions"/> to <see cref="StringSegmentSplitOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="StringSplitOptions"/> value to be converted to <see cref="StringSegmentSplitOptions"/>.</param>
        /// <returns>
        /// A <see cref="StringSegmentSplitOptions"/> instance that represents the specified <paramref name="options"/>.
        /// </returns>
        public static implicit operator StringSegmentSplitOptions(StringSplitOptions options) => new StringSegmentSplitOptions((Options)options);

        /// <summary>
        /// Performs an explicit conversion from <see cref="StringSegmentSplitOptions"/> to <see cref="StringSplitOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="StringSegmentSplitOptions"/> value to be converted to <see cref="StringSplitOptions"/>.</param>
        /// <returns>
        /// A <see cref="StringSplitOptions"/> instance that represents the specified <paramref name="options"/>.
        /// </returns>
        public static explicit operator StringSplitOptions(StringSegmentSplitOptions options) => (StringSplitOptions)options.value;

        /// <summary>
        /// Performs an implicit conversion from <see cref="bool">bool</see> to <see cref="StringSegmentSplitOptions"/>.
        /// <br/>This member is obsolete and is specified to provide compatibility with the old <see cref="O:KGySoft.CoreLibraries.StringSegment.Split">StringSegment.Split</see> overloads,
        /// which used to specify a boolean <paramref name="removeEmptyEntries"/> argument in place of the new <see cref="StringSegmentSplitOptions"/> type.
        /// </summary>
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to return <see cref="RemoveEmptyEntries"/>; <see langword="false"/>&#160;to return <see cref="None"/>.</param>
        /// <returns>
        /// A <see cref="StringSegmentSplitOptions"/> instance that represents the value of the specified <paramref name="removeEmptyEntries"/> parameter.
        /// </returns>
        [Obsolete("This operator is maintained to be compatible with the earlier removeEmptyEntries bool parameters. Use the StringSegmentSplitOptions flags instead.")]
        public static implicit operator StringSegmentSplitOptions(bool removeEmptyEntries) => new StringSegmentSplitOptions(removeEmptyEntries ? Options.RemoveEmptyEntries : Options.None);

        /// <summary>
        /// Performs an explicit conversion from <see cref="int"/> to <see cref="StringSegmentSplitOptions"/>.
        /// </summary>
        /// <param name="value">The integer value to be converted to <see cref="StringSegmentSplitOptions"/>.</param>
        /// <returns>
        /// A <see cref="StringSegmentSplitOptions"/> instance that has the specified underlying <paramref name="value"/>.
        /// </returns>
        public static explicit operator StringSegmentSplitOptions(int value) => new StringSegmentSplitOptions((Options)value);

        /// <summary>
        /// Performs an explicit conversion from <see cref="StringSegmentSplitOptions"/> to <see cref="int"/>.
        /// </summary>
        /// <param name="options">The options to be converted to <see cref="int"/>.</param>
        /// <returns>
        /// The underlying <see cref="int"/> value of the specified <paramref name="options"/>.
        /// </returns>
        public static explicit operator int(StringSegmentSplitOptions options) => (int)options.value;

        /// <summary>
        /// Performs a bitwise OR operation on the provided operands.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>The result of the bitwise OR operation.</returns>
        public static StringSegmentSplitOptions operator |(StringSegmentSplitOptions left, StringSegmentSplitOptions right) => new StringSegmentSplitOptions(left.value | right.value);

        /// <summary>
        /// Performs a bitwise AND operation on the provided operands.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>The result of the bitwise AND operation.</returns>
        public static StringSegmentSplitOptions operator &(StringSegmentSplitOptions left, StringSegmentSplitOptions right) => new StringSegmentSplitOptions(left.value & right.value);

        /// <summary>
        /// Performs a bitwise NOT operation on the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to be negated.</param>
        /// <returns>The result of the bitwise NOT operation.</returns>
        public static StringSegmentSplitOptions operator ~(StringSegmentSplitOptions value) => new StringSegmentSplitOptions(~value.value);

        #endregion

        #region Constructors

        private StringSegmentSplitOptions(Options options) => value = options;

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Indicates whether the current <see cref="StringSegmentSplitOptions"/> instance is equal to another one specified in the <paramref name="other"/> parameter.
        /// </summary>
        /// <param name="other">An <see cref="StringSegmentSplitOptions"/> instance to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the current object is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.</returns>
        public bool Equals(StringSegmentSplitOptions other) => value == other.value;

        /// <summary>
        /// Determines whether the specified <see cref="object">object</see> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the specified object is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj) => obj is StringSegmentSplitOptions other && Equals(other);

        /// <summary>
        /// Returns a hash code for this <see cref="StringSegmentSplitOptions"/> instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode() => (int)value;

        /// <summary>
        /// Gets the string representation of this <see cref="StringSegmentSplitOptions"/> instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this <see cref="StringSegmentSplitOptions"/> instance.
        /// </returns>
        public override string ToString() => Enum<Options>.ToString(value);

        #endregion

        #region Internal Methods

        internal void AssertValid()
        {
            if (!value.AllFlagsDefined())
                Throw.ArgumentOutOfRangeException(Argument.options, Res.ArgumentOutOfRange);
        }

        #endregion

        #endregion
    }
}
