#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegmentExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2020 - All Rights Reserved
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
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.CoreLibraries
{
    public static class StringSegmentExtensions
    {
        #region Fields

        private static readonly char[] newLineSeparators = { '\r', '\n' };

        #endregion

        #region Methods

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the next whitespace character and returns
        /// the consumed part without the whitespace. If the first character of <paramref name="rest"/> was a whitespace
        /// before the call, then an empty segment is returned. If the whole string is consumed, then <paramref name="rest"/>
        /// will be <see cref="StringSegment.Null"/>.
        /// </summary>
        /// <param name="rest">The rest.</param>
        /// <returns></returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToWhiteSpace(ref this StringSegment rest)
            => StringSegment.GetNextSegment(ref rest);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, char separator)
            => StringSegment.GetNextSegment(ref rest, separator);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, StringSegment separator)
        {
            if (separator.Length == 0)
            {
                if (separator.IsNull)
                    Throw.ArgumentNullException(Argument.separator);
                StringSegment result = rest;
                rest = default;
                return result;
            }

            return StringSegment.GetNextSegment(ref rest, separator);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, string separator)
        {
            if (separator == null)
                Throw.ArgumentNullException(Argument.separator);
            if (separator.Length == 0)
            {
                StringSegment result = rest;
                rest = default;
                return result;
            }

            return StringSegment.GetNextSegment(ref rest, separator);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, params char[] separators)
        {
            if (separators == null)
                Throw.ArgumentNullException(Argument.separators);
            if (separators.Length <= 1)
            {
                if (separators.Length == 1)
                    return ReadToSeparator(ref rest, separators[0]);

                StringSegment result = rest;
                rest = default;
                return result;
            }

            return StringSegment.GetNextSegment(ref rest, separators);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, params string[] separators)
        {
            if (separators == null)
                Throw.ArgumentNullException(Argument.separators);
            if (separators.Length <= 1)
            {
                if (separators.Length == 1)
                {
                    string separator = separators[0];
                    if (!String.IsNullOrEmpty(separator))
                        return StringSegment.GetNextSegment(ref rest, separator);
                }

                StringSegment result = rest;
                rest = default;
                return result;
            }

            return StringSegment.GetNextSegment(ref rest, separators);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, params StringSegment[] separators)
        {
            if (separators == null)
                Throw.ArgumentNullException(Argument.separators);
            if (separators.Length <= 1)
            {
                if (separators.Length == 1)
                {
                    StringSegment separator = separators[0];
                    if (!separator.IsNullOrEmpty)
                        return StringSegment.GetNextSegment(ref rest, separator);
                }

                StringSegment result = rest;
                rest = default;
                return result;
            }

            return StringSegment.GetNextSegment(ref rest, separators);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadLine(ref this StringSegment rest)
        {
            // looking for chars is much faster than using { "\r\n", "\r", "\n" } separators
            StringSegment result = StringSegment.GetNextSegment(ref rest, newLineSeparators);

            // if we found a '\r' we check whether it is followed by a '\n'
            if (rest.Length == 0 || rest.UnderlyingString[rest.Offset - 1] != '\r' || rest.GetCharInternal(0) != '\n')
                return result;
            rest = rest.SubstringInternal(1);
            return result;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment Read(ref this StringSegment rest, int maxLength)
        {
            if (maxLength < 0)
                Throw.ArgumentOutOfRangeException(Argument.maxLength, Res.ArgumentMustBeLessThanOrEqualTo(0));

            StringSegment result;
            if (maxLength >= rest.Length)
            {
                result = rest;
                rest = default;
                return result;
            }

            result = rest.SubstringInternal(0, maxLength);
            rest = rest.SubstringInternal(maxLength);
            return result;
        }

        #endregion
    }
}
