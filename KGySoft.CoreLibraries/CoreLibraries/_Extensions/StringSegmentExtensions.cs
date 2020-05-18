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

using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.CoreLibraries
{
    public static class StringSegmentExtensions
    {
        #region Fields

        private static readonly string[] newLineSeparators = { "\r\n", "\r", "\n" };

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
            // TODO: handle trivial cases here (see asserts and conditions from Split)
            => StringSegment.GetNextSegment(ref rest);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, char separator)
            // TODO: handle trivial cases here (see asserts and conditions from Split)
            => StringSegment.GetNextSegment(ref rest, separator);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, StringSegment separator)
            // TODO: handle trivial cases here (see asserts and conditions from Split)
            => StringSegment.GetNextSegment(ref rest, separator);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, string separator)
            // TODO: handle trivial cases here (see asserts and conditions from Split)
            => StringSegment.GetNextSegment(ref rest, separator);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, char[] separator)
            // TODO: handle trivial cases here (see asserts and conditions from Split)
            => StringSegment.GetNextSegment(ref rest, separator);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, string[] separator)
            // TODO: handle trivial cases here (see asserts and conditions from Split)
            => StringSegment.GetNextSegment(ref rest, separator);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, StringSegment[] separator)
            // TODO: handle trivial cases here (see asserts and conditions from Split)
            => StringSegment.GetNextSegment(ref rest, separator);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadLine(ref this StringSegment rest)
            => StringSegment.GetNextSegment(ref rest, newLineSeparators);

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
