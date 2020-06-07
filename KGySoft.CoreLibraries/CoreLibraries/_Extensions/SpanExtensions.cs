#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SpanExtensions.cs
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

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> types.
    /// </summary>
    /// <remarks><note>This class is available in .NET Core 3.0/.NET Standard 2.1 and above.</note></remarks>
    internal static class SpanExtensions
    {
        #region Methods

        #region Internal Methods

        internal static bool TryParseIntQuick(this ReadOnlySpan<char> s, bool allowNegative, ulong max, out ulong result)
        {
            Debug.Assert(s.Length > 0, $"Nonzero length is expected in {nameof(TryParseIntQuick)}");

            result = 0UL;
            bool isNegative = false;
            int i = 0;

            switch (s[0])
            {
                case '+':
                    i += 1;
                    break;
                case '-':
                    if (!allowNegative)
                        return false;
                    isNegative = true;
                    i += 1;
                    break;
            }

            ulong value = 0UL;
            while (i < s.Length)
            {
                uint digit = s[i] - (uint)'0';
                if (digit > 9)
                    return false;

                ulong newValue = value * 10 + digit;

                // overflow
                if (newValue < value)
                    return false;

                value = newValue;
                i += 1;
            }

            // we check it only here to minimize the performance overhead for valid cases
            if (value > max && !(isNegative && value == max + 1))
                return false;

            result = isNegative ? (ulong)-(long)value : value;
            return true;
        }

        internal static bool TryGetNextSegment(this ref ReadOnlySpan<char> s, ReadOnlySpan<char> separator, out ReadOnlySpan<char> result)
        {
            if (s.Length == 0)
            {
                result = default;
                return false;
            }

            int pos = s.IndexOf(separator);

            // last segment
            if (pos == -1)
            {
                result = s;
                s = default;
                return true;
            }

            // returning next segment and advance
            result = s.Slice(0, pos);
            s = s.Slice(pos + separator.Length);
            return true;
        }

        #endregion

        #endregion
    }
}
#endif