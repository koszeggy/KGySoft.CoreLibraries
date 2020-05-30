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
    /// <summary>
    /// Contains extension methods for the <see cref="StringSegment"/> type.
    /// </summary>
    public static class StringSegmentExtensions
    {
        #region Fields

        private static readonly char[] newLineSeparators = { '\r', '\n' };

        #endregion

        #region Methods

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the next whitespace character and returns
        /// the consumed part without the whitespace. If the first character of <paramref name="rest"/> was a whitespace
        /// before the call, then an empty segment is returned. If the whole <see cref="StringSegment"/> has been processed, then <paramref name="rest"/>
        /// will be <see cref="StringSegment.Null">StringSegment.Null</see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="StringSegment.Null">StringSegment.Null</see> if the whole segment has been processed.</param>
        /// <returns>A <see cref="StringSegment"/> that contains the first segment of the original value of the <paramref name="rest"/> parameter delimited by whitespace characters,
        /// or the complete original value of <paramref name="rest"/> if it contained no more whitespace characters.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToWhiteSpace(ref this StringSegment rest)
            => StringSegment.GetNextSegment(ref rest);

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the next <paramref name="separator"/> character and returns
        /// the consumed part without the <paramref name="separator"/>. If the first character of <paramref name="rest"/> was a <paramref name="separator"/>
        /// before the call, then an empty segment is returned. If the whole <see cref="StringSegment"/> has been processed, then <paramref name="rest"/>
        /// will be <see cref="StringSegment.Null">StringSegment.Null</see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="StringSegment.Null">StringSegment.Null</see> if the whole segment has been processed.</param>
        /// <param name="separator">The separator character to search in the specified <see cref="StringSegment"/>.</param>
        /// <returns>A <see cref="StringSegment"/> that contains the first segment of the original value of the <paramref name="rest"/> parameter delimited by the specified <paramref name="separator"/>,
        /// or the complete original value of <paramref name="rest"/> if it contained no more separators.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, char separator)
            => StringSegment.GetNextSegment(ref rest, separator);

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the next <paramref name="separator"/> and returns
        /// the consumed part without the <paramref name="separator"/>. If <paramref name="rest"/> started with <paramref name="separator"/>
        /// before the call, then an empty segment is returned. If the whole <see cref="StringSegment"/> has been processed, then <paramref name="rest"/>
        /// will be <see cref="StringSegment.Null">StringSegment.Null</see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="StringSegment.Null">StringSegment.Null</see> if the whole segment has been processed.</param>
        /// <param name="separator">The separator segment to search in the specified <see cref="StringSegment"/>.</param>
        /// <returns>A <see cref="StringSegment"/> that contains the first segment of the original value of the <paramref name="rest"/> parameter delimited by the specified <paramref name="separator"/>,
        /// or the complete original value of <paramref name="rest"/> if it contained no more separators.</returns>
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

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the next <paramref name="separator"/> and returns
        /// the consumed part without the <paramref name="separator"/>. If <paramref name="rest"/> started with <paramref name="separator"/>
        /// before the call, then an empty segment is returned. If the whole <see cref="StringSegment"/> has been processed, then <paramref name="rest"/>
        /// will be <see cref="StringSegment.Null">StringSegment.Null</see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="StringSegment.Null">StringSegment.Null</see> if the whole segment has been processed.</param>
        /// <param name="separator">The separator string to search in the specified <see cref="StringSegment"/>.</param>
        /// <returns>A <see cref="StringSegment"/> that contains the first segment of the original value of the <paramref name="rest"/> parameter delimited by the specified <paramref name="separator"/>,
        /// or the complete original value of <paramref name="rest"/> if it contained no more separators.</returns>
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

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the next separator and returns
        /// the consumed part without the separator. If <paramref name="rest"/> started with one of the <paramref name="separators"/>
        /// before the call, then an empty segment is returned. If the whole <see cref="StringSegment"/> has been processed, then <paramref name="rest"/>
        /// will be <see cref="StringSegment.Null">StringSegment.Null</see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="StringSegment.Null">StringSegment.Null</see> if the whole segment has been processed.</param>
        /// <param name="separators">The separators to search in the specified <see cref="StringSegment"/>.</param>
        /// <returns>A <see cref="StringSegment"/> that contains the first segment of the original value of the <paramref name="rest"/> parameter delimited by any of the specified <paramref name="separators"/>,
        /// or the complete original value of <paramref name="rest"/> if it contained no more separators.</returns>
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

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the next separator and returns
        /// the consumed part without the separator. If <paramref name="rest"/> started with one of the <paramref name="separators"/>
        /// before the call, then an empty segment is returned. If the whole <see cref="StringSegment"/> has been processed, then <paramref name="rest"/>
        /// will be <see cref="StringSegment.Null">StringSegment.Null</see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="StringSegment.Null">StringSegment.Null</see> if the whole segment has been processed.</param>
        /// <param name="separators">The separators to search in the specified <see cref="StringSegment"/>.</param>
        /// <returns>A <see cref="StringSegment"/> that contains the first segment of the original value of the <paramref name="rest"/> parameter delimited by any of the specified <paramref name="separators"/>,
        /// or the complete original value of <paramref name="rest"/> if it contained no more separators.</returns>
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

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the next separator and returns
        /// the consumed part without the separator. If <paramref name="rest"/> started with one of the <paramref name="separators"/>
        /// before the call, then an empty segment is returned. If the whole <see cref="StringSegment"/> has been processed, then <paramref name="rest"/>
        /// will be <see cref="StringSegment.Null">StringSegment.Null</see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="StringSegment.Null">StringSegment.Null</see> if the whole segment has been processed.</param>
        /// <param name="separators">The separators to search in the specified <see cref="StringSegment"/>.</param>
        /// <returns>A <see cref="StringSegment"/> that contains the first segment of the original value of the <paramref name="rest"/> parameter delimited by any of the specified <paramref name="separators"/>,
        /// or the complete original value of <paramref name="rest"/> if it contained no more separators.</returns>
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

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the current line and returns
        /// the consumed part without the newline character(s). If <paramref name="rest"/> started with a new line
        /// before the call, then an empty segment is returned. If the whole <see cref="StringSegment"/> has been processed, then <paramref name="rest"/>
        /// will be <see cref="StringSegment.Null">StringSegment.Null</see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="StringSegment.Null">StringSegment.Null</see> if the whole segment has been processed.</param>
        /// <returns>A <see cref="StringSegment"/> that contains the first line of the original value of the <paramref name="rest"/> parameter,
        /// or the complete original value of <paramref name="rest"/> if it contained no more lines.</returns>
        /// <remarks>
        /// <para>The effect of this method is the same as calling the <see cref="ReadToSeparator(ref StringSegment, string[])"/> method with <c><![CDATA["\r\n", "\r", "\n"]]></c>
        /// parameters but it is implemented a bit more optimized way.</para>
        /// </remarks>
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

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter consuming up to <paramref name="maxLength"/> characters and returns
        /// the consumed part. If <paramref name="rest"/> started with a new line
        /// before the call, then an empty segment is returned. If the whole <see cref="StringSegment"/> has been processed, then <paramref name="rest"/>
        /// will be <see cref="StringSegment.Null">StringSegment.Null</see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="StringSegment.Null">StringSegment.Null</see> if the whole segment has been processed.</param>
        /// <param name="maxLength">The maximum number of characters to read.</param>
        /// <returns>A <see cref="StringSegment"/> that contains the first line of the original value of the <paramref name="rest"/> parameter,
        /// or the complete original value of <paramref name="rest"/> if it contained no more than <paramref name="maxLength"/> characters.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment Read(ref this StringSegment rest, int maxLength)
        {
            if (maxLength <= 0)
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
