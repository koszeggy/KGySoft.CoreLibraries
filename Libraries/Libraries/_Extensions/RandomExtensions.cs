#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2018 - All Rights Reserved
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using KGySoft.Libraries.Resources;

#endregion

namespace KGySoft.Libraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="Random"/> type.
    /// </summary>
    public static class RandomExtensions
    {
        #region Constants

        private const string digits = "0123456789";
        private const string lowerCaseLetters = "abcdefghijklmnopqrstuvwxyz";
        private const string upperCaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string letters = lowerCaseLetters + upperCaseLetters;
        private const string lettersAndDigits = digits + letters;

        #endregion

        #region Fields

        private static readonly string ascii = new String(Enumerable.Range(32, 95).Select(i => (char)i).ToArray());

        #endregion

        #region Methods

        #region Boolean

        /// <summary>
        /// Returns a random <see cref="bool"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A <see cref="bool"/> value that is either <c>true</c> or <c>false</c>.</returns>
        public static bool NextBoolean(this Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            return (random.Next() & 1) == 0;
        }

        #endregion

        #region Integers

        /// <summary>
        /// Returns a random <see cref="sbyte"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>An 8-bit signed integer that is greater than or equal to <see cref="sbyte.MinValue">SByte.MinValue</see> and less or equal to <see cref="sbyte.MaxValue">SByte.MaxValue</see>.</returns>
        public static sbyte NextSByte(this Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            byte[] buf = new byte[1];
            random.NextBytes(buf);
            return (sbyte)buf[0];
        }

        /// <summary>
        /// Returns a random <see cref="sbyte"/> value that is less or equal to the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>An 8-bit signed integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        public static sbyte NextSByte(this Random random, sbyte maxValue, bool inclusiveUpperBound = false)
            => (sbyte)random.NextInt64(0L, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="sbyte"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>An 8-bit signed integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        public static sbyte NextSByte(this Random random, sbyte minValue, sbyte maxValue, bool inclusiveUpperBound = false)
            => (sbyte)random.NextInt64(minValue, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="byte"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>An 8-bit unsigned integer that is greater than or equal to 0 and less or equal to <see cref="byte.MaxValue">Byte.MaxValue</see>.</returns>
        public static byte NextByte(this Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            byte[] buf = new byte[1];
            random.NextBytes(buf);
            return buf[0];
        }

        /// <summary>
        /// Returns a random <see cref="byte"/> value that is less or equal to the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>An 8-bit unsigned integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        public static byte NextByte(this Random random, byte maxValue, bool inclusiveUpperBound = false)
            => (byte)random.NextUInt64(0UL, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="byte"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>An 8-bit unsigned integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        public static byte NextByte(this Random random, byte minValue, byte maxValue, bool inclusiveUpperBound = false)
            => (byte)random.NextUInt64(minValue, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="short"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 16-bit signed integer that is greater than or equal to <see cref="short.MinValue">Int16.MinValue</see> and less or equal to <see cref="short.MaxValue">Int16.MaxValue</see>.</returns>
        /// <remarks>Similarly to the <see cref="Random.Next()">Random.Next()</see> method this one returns an <see cref="int"/> value; however, the result can be negative and
        /// the maximum possible value can be <see cref="int.MaxValue">Int32.MaxValue</see>.</remarks>
        public static short NextInt16(this Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            byte[] buf = new byte[2];
            random.NextBytes(buf);
            return BitConverter.ToInt16(buf, 0);
        }

        /// <summary>
        /// Returns a random <see cref="short"/> value that is less or equal to the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>A 16-bit signed integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        public static short NextInt16(this Random random, short maxValue, bool inclusiveUpperBound = false)
            => (short)random.NextInt64(0L, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="short"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>A 16-bit signed integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        public static short NextInt16(this Random random, short minValue, short maxValue, bool inclusiveUpperBound = false)
            => (short)random.NextInt64(minValue, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="ushort"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 16-bit unsigned integer that is greater than or equal to 0 and less or equal to <see cref="ushort.MaxValue">UInt16.MaxValue</see>.</returns>
        public static ushort NextUInt16(this Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            byte[] buf = new byte[2];
            random.NextBytes(buf);
            return BitConverter.ToUInt16(buf, 0);
        }

        /// <summary>
        /// Returns a random <see cref="ushort"/> value that is less or equal to the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>A 16-bit unsigned integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        public static ushort NextUInt16(this Random random, ushort maxValue, bool inclusiveUpperBound = false)
            => (ushort)random.NextUInt64(0UL, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="ushort"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>A 16-bit unsigned integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        public static ushort NextUInt16(this Random random, ushort minValue, ushort maxValue, bool inclusiveUpperBound = false)
            => (ushort)random.NextUInt64(minValue, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="int"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to <see cref="int.MinValue">Int32.MinValue</see> and less or equal to <see cref="int.MaxValue">Int32.MaxValue</see>.</returns>
        /// <remarks>Similarly to the <see cref="Random.Next()">Random.Next()</see> method this one returns an <see cref="int"/> value; however, the result can be negative and
        /// the maximum possible value can be <see cref="int.MaxValue">Int32.MaxValue</see>.</remarks>
        public static int NextInt32(this Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            byte[] buf = new byte[4];
            random.NextBytes(buf);
            return BitConverter.ToInt32(buf, 0);
        }

        /// <summary>
        /// Returns a random <see cref="int"/> value that is less or equal to the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        public static int NextInt32(this Random random, int maxValue, bool inclusiveUpperBound = false)
            => (int)random.NextInt64(0L, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="int"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        public static int NextInt32(this Random random, int minValue, int maxValue, bool inclusiveUpperBound = false)
            => (int)random.NextInt64(minValue, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="uint"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 32-bit unsigned integer that is greater than or equal to 0 and less or equal to <see cref="uint.MaxValue">UInt32.MaxValue</see>.</returns>
        public static uint NextUInt32(this Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            byte[] buf = new byte[4];
            random.NextBytes(buf);
            return BitConverter.ToUInt32(buf, 0);
        }

        /// <summary>
        /// Returns a random <see cref="uint"/> value that is less or equal to the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>A 32-bit unsigned integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        public static uint NextUInt32(this Random random, uint maxValue, bool inclusiveUpperBound = false)
            => (uint)random.NextUInt64(0UL, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="uint"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>A 32-bit unsigned integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        public static uint NextUInt32(this Random random, uint minValue, uint maxValue, bool inclusiveUpperBound = false)
            => (uint)random.NextUInt64(minValue, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="long"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 64-bit unsigned integer that is greater than or equal to <see cref="long.MinValue">Int64.MinValue</see> and less or equal to <see cref="long.MaxValue">Int64.MaxValue</see>.</returns>
        public static long NextInt64(this Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            byte[] buf = new byte[8];
            random.NextBytes(buf);
            return BitConverter.ToInt64(buf, 0);
        }

        /// <summary>
        /// Returns a random <see cref="long"/> value that is less or equal to the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        public static long NextInt64(this Random random, long maxValue, bool inclusiveUpperBound = false) 
            => random.NextInt64(0L, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="long"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        public static long NextInt64(this Random random, long minValue, long maxValue, bool inclusiveUpperBound = false)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            if (minValue == maxValue)
                return minValue;

            if (maxValue < minValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue), Res.Get(Res.ArgumentOutOfRange));

            ulong range = (ulong)(maxValue - minValue);
            if (inclusiveUpperBound)
            {
                if (range == ulong.MaxValue)
                    return random.NextInt64();
                range++;
            }

            ulong limit = ulong.MaxValue - (ulong.MaxValue % range);
            ulong r;
            do
            {
                r = random.NextUInt64();
            }
            while (r > limit);
            return (long)((r % range) + (ulong)minValue);
        }

        /// <summary>
        /// Returns a random <see cref="ulong"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 64-bit unsigned integer that is greater than or equal to 0 and less or equal to <see cref="ulong.MaxValue">UInt64.MaxValue</see>.</returns>
        public static ulong NextUInt64(this Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            byte[] buf = new byte[8];
            random.NextBytes(buf);
            return BitConverter.ToUInt64(buf, 0);
        }

        /// <summary>
        /// Returns a random <see cref="ulong"/> value that is less or equal to the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>A 64-bit unsigned integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        public static ulong NextUInt64(this Random random, ulong maxValue, bool inclusiveUpperBound = false)
            => random.NextUInt64(0UL, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="ulong"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><c>true</c> to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <c>false</c>. This parameter is optional.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>A 64-bit unsigned integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> if <c>false</c>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        public static ulong NextUInt64(this Random random, ulong minValue, ulong maxValue, bool inclusiveUpperBound = false)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            if (minValue == maxValue)
                return minValue;

            if (maxValue < minValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue), Res.Get(Res.ArgumentOutOfRange));

            ulong range = maxValue - minValue;
            if (inclusiveUpperBound)
            {
                if (range == ulong.MaxValue)
                    return random.NextUInt64();
                range++;
            }

            ulong limit = ulong.MaxValue - (ulong.MaxValue % range);
            ulong r;
            do
            {
                r = random.NextUInt64();
            }
            while (r > limit);

            return (r % range) + minValue;
        }

        #endregion

        #region Floating-point types

        /// <summary>
        /// Returns a random <see cref="float"/> value that is greater than or equal to 0.0 and less than 1.0.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A single-precision floating point number that is greater than or equal to 0.0 and less than 1.0.</returns>
        public static float NextFloat(this Random random)
            => (float)random.NextDouble();

        /// <summary>
        /// Returns a random <see cref="float"/> value that is less or equal to the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="RandomScale.Auto"/>.</param>
        /// <returns>A single-precision floating point number that is greater than or equal to 0.0 and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases.
        /// With <see cref="RandomScale.ForceLinear"/> <paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.
        /// </para>
        /// </remarks>
        public static float NextFloat(this Random random, float maxValue, RandomScale scale = RandomScale.Auto)
            => random.NextFloat(0f, maxValue, scale);

        /// <summary>
        /// Returns a random <see cref="float"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="RandomScale.Auto"/>.</param>
        /// <returns>A single-precision floating point number that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases such as
        /// when <paramref name="minValue"/> is equal to <paramref name="maxValue"/> or when integer parts of both limits are beyond the precision of the <see cref="double"/> type.</para>
        /// With <see cref="RandomScale.ForceLinear"/> <paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.
        /// </remarks>
        public static float NextFloat(this Random random, float minValue, float maxValue, RandomScale scale = RandomScale.Auto)
        {
            float AdjustValue(float value) => Single.IsNegativeInfinity(value) ? Single.MinValue : (Single.IsPositiveInfinity(value) ? Single.MaxValue : value);

            // both are the same infinity
            if (Single.IsPositiveInfinity(minValue) && Single.IsPositiveInfinity(maxValue)
                || Single.IsNegativeInfinity(minValue) && Single.IsNegativeInfinity(maxValue))
                throw new ArgumentOutOfRangeException(nameof(minValue), Res.Get(Res.ArgumentOutOfRange));

            return (float)random.NextDouble(AdjustValue(minValue), AdjustValue(maxValue), scale);
        }

        /// <summary>
        /// Returns a random <see cref="double"/> value that is less or equal to the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="RandomScale.Auto"/>.</param>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0 and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases.
        /// With <see cref="RandomScale.ForceLinear"/> <paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.
        /// </para>
        /// </remarks>
        public static double NextDouble(this Random random, double maxValue, RandomScale scale = RandomScale.Auto)
            => random.NextDouble(0d, maxValue, scale);

        /// <summary>
        /// Returns a random <see cref="double"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="RandomScale.Auto"/>.</param>
        /// <returns>A double-precision floating point number that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases such as
        /// when <paramref name="minValue"/> is equal to <paramref name="maxValue"/> or when integer parts of both limits are beyond the precision of the <see cref="double"/> type.</para>
        /// With <see cref="RandomScale.ForceLinear"/> <paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.
        /// </remarks>
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static double NextDouble(this Random random, double minValue, double maxValue, RandomScale scale = RandomScale.Auto)
        {
            double AdjustValue(double value) => Double.IsNegativeInfinity(value) ? Double.MinValue : (Double.IsPositiveInfinity(value) ? Double.MaxValue : value);

            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));

            // both are the same infinity
            if ((Double.IsPositiveInfinity(minValue) && Double.IsPositiveInfinity(maxValue) || Double.IsNegativeInfinity(minValue) && Double.IsNegativeInfinity(maxValue))
                // or any of them is NaN
                || Double.IsNaN(minValue) || Double.IsNaN(maxValue)
                // or max < min
                || maxValue < minValue)
                throw new ArgumentOutOfRangeException(Double.IsNaN(maxValue) || maxValue < minValue ? nameof(maxValue) : nameof(minValue), Res.Get(Res.ArgumentOutOfRange));

            if (!Enum<RandomScale>.IsDefined(scale))
                throw new ArgumentOutOfRangeException(nameof(scale), Res.Get(Res.ArgumentOutOfRange));

            minValue = AdjustValue(minValue);
            maxValue = AdjustValue(maxValue);
            if (minValue == maxValue)
                return minValue;

            bool posAndNeg = minValue < 0d && maxValue > 0d;
            double minAbs = Math.Min(Math.Abs(minValue), Math.Abs(maxValue));
            double maxAbs = Math.Max(Math.Abs(minValue), Math.Abs(maxValue));

            // if linear scaling is forced...
            if (scale == RandomScale.ForceLinear
                // or we use auto scaling and maximum is UInt16 or when the difference of order of magnitude is smaller than 4
                || (scale == RandomScale.Auto && (maxAbs <= ushort.MaxValue || !posAndNeg && maxAbs < minAbs * 16)))
            {
                return NextDoubleLinear(random, minValue, maxValue);
            }

            int sign;
            if (!posAndNeg)
                sign = minValue < 0d ? -1 : 1;
            else
            {
                // if both negative and positive results are expected we select the sign based on the size of the ranges
                double sample = random.NextDouble();
                var rate = minAbs / maxAbs;
                var absMinValue = Math.Abs(minValue);
                bool isNeg = absMinValue <= maxValue
                    ? rate / 2d > sample
                    : rate / 2d < sample;
                sign = isNeg ? -1 : 1;

                // now adjusting the limits for 0..[selected range]
                minAbs = 0d;
                maxAbs = isNeg ? absMinValue : Math.Abs(maxValue);
            }

            // Possible double exponents are -1022..1023 but we don't generate too small exponents for big ranges because
            // that would cause too many almost zero results, which are much smaller than the original NextDouble values.
            double minExponent = minAbs == 0d ? -16d : Math.Log(minAbs, 2d);
            double maxExponent = Math.Log(maxAbs, 2d);
            if (minExponent == maxExponent)
                return minValue;

            // We decrease exponents only if the given range is already small. Even lower than -1022 is no problem, the result may be 0
            if (maxExponent < minExponent)
                minExponent = maxExponent - 4;

            double result = sign * Math.Pow(2d, NextDoubleLinear(random, minExponent, maxExponent));

            // protecting ourselves against inaccurate calculations; however, in practice result is always in range.
            return result < minValue ? minValue : (result > maxValue ? maxValue : result);
        }

        /// <summary>
        /// Returns a random <see cref="decimal"/> value that is greater than or equal to 0.0 and less than 1.0.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A decimal floating point number that is greater than or equal to 0.0 and less than 1.0.</returns>
        public static decimal NextDecimal(this Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));

            decimal result;
            do
            {
                // The hi argument of 0.9999999999999999999999999999m is 542101086.
                // (MaxInt, MaxInt, 542101086) is actually bigger than 1 but in practice the loop almost never repeats.
                result = new Decimal(random.NextInt32(), random.NextInt32(), random.Next(542101087), false, 28);
            } while (result >= 1m);

            return result;
        }

        /// <summary>
        /// Returns a random <see cref="decimal"/> value that is less or equal to the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="RandomScale.Auto"/>.</param>
        /// <returns>A decimal floating point number that is greater than or equal to 0.0 and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases.
        /// With <see cref="RandomScale.ForceLinear"/> <paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.
        /// </para>
        /// </remarks>
        public static decimal NextDecimal(this Random random, decimal maxValue, RandomScale scale = RandomScale.Auto)
            => NextDecimal(random, 0m, maxValue, scale);

        /// <summary>
        /// Returns a random <see cref="decimal"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="RandomScale.Auto"/>.</param>
        /// <returns>A single-precision floating point number that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases such as
        /// when <paramref name="minValue"/> is equal to <paramref name="maxValue"/>.</para>
        /// With <see cref="RandomScale.ForceLinear"/> <paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.
        /// </remarks>
        public static decimal NextDecimal(this Random random, decimal minValue, decimal maxValue, RandomScale scale = RandomScale.Auto)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));

            if (maxValue < minValue)
                throw new ArgumentOutOfRangeException(nameof(maxValue), Res.Get(Res.ArgumentOutOfRange));

            if (!Enum<RandomScale>.IsDefined(scale))
                throw new ArgumentOutOfRangeException(nameof(scale), Res.Get(Res.ArgumentOutOfRange));

            if (minValue == maxValue)
                return minValue;

            bool posAndNeg = minValue < 0m && maxValue > 0m;
            decimal minAbs = Math.Min(Math.Abs(minValue), Math.Abs(maxValue));
            decimal maxAbs = Math.Max(Math.Abs(minValue), Math.Abs(maxValue));

            // if linear scaling is forced...
            if (scale == RandomScale.ForceLinear
                // or we use auto scaling and maximum is UInt16 or when the difference of order of magnitude is smaller than 4
                || (scale == RandomScale.Auto && (maxAbs <= ushort.MaxValue || !posAndNeg && maxAbs / 16m < minAbs)))
            {
                return NextDecimalLinear(random, minValue, maxValue);
            }

            int sign;
            if (!posAndNeg)
                sign = minValue < 0m ? -1 : 1;
            else
            {
                // if both negative and positive results are expected we select the sign based on the size of the ranges
                decimal sample = random.NextDecimal();
                var rate = minAbs / maxAbs;
                var absMinValue = Math.Abs(minValue);
                bool isNeg = absMinValue <= maxValue
                    ? rate / 2m > sample
                    : rate / 2m < sample;
                sign = isNeg ? -1 : 1;

                // now adjusting the limits for 0..[selected range]
                minAbs = 0m;
                maxAbs = isNeg ? absMinValue : Math.Abs(maxValue);
            }

            // We don't generate too small exponents for big ranges because
            // that would cause too many almost zero results
            decimal minExponent = minAbs == 0m ? -5m : minAbs.Log10();
            decimal maxExponent = maxAbs.Log10();
            if (minExponent.Equals(maxExponent))
                return minValue;

            // We decrease exponents only if the given range is already small.
            if (maxExponent < minExponent)
                minExponent = maxExponent - 4;

            decimal result;
            do
            {
                result = sign * 10m.Pow(NextDecimalLinear(random, minExponent, maxExponent));
            } while (result < minValue || result > maxValue);
            return result;
        }

        #endregion

        #region Char/String

        /// <summary>
        /// Returns a random <see cref="char"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random character returned.</param>
        /// <param name="maxValue">The inclusive upper bound of the random character returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <returns>A <see cref="char"/> value that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.</returns>
        public static char NextChar(this Random random, char minValue = Char.MinValue, char maxValue = Char.MaxValue)
            => minValue == Char.MinValue && maxValue == Char.MaxValue
                ? (char)random.NextUInt16()
                : (char)random.NextUInt64(minValue, maxValue, true);

        /// <summary>
        /// Returns a random <see cref="string"/> that has the length between the specified range and consists of the specified <paramref name="allowedCharacters"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minLength">The inclusive lower bound of the length of the returned string.</param>
        /// <param name="maxLength">The inclusive upper bound of the length of the returned string. Must be greater or equal to <paramref name="minLength"/>.</param>
        /// <param name="allowedCharacters">A string containing the allowed characters. Recurrence is not checked.</param>
        /// <returns>A <see cref="string"/> value that has the length greater than or equal to <paramref name="minLength"/> and less and less or equal to <paramref name="maxLength"/>
        /// and contains only the specified characters.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="allowedCharacters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> is less than 0 or <paramref name="maxLength"/> is less than <paramref name="minLength"/></exception>
        /// <exception cref="ArgumentException"><paramref name="allowedCharacters"/> is empty.</exception>
        public static string NextString(this Random random, int minLength, int maxLength, string allowedCharacters)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            if (minLength < 0)
                throw new ArgumentOutOfRangeException(nameof(minLength), Res.Get(Res.ArgumentOutOfRange));
            if (maxLength < minLength)
                throw new ArgumentOutOfRangeException(nameof(maxLength), Res.Get(Res.ArgumentOutOfRange));
            if (allowedCharacters == null)
                throw new ArgumentNullException(nameof(allowedCharacters), Res.Get(Res.ArgumentNull));
            if (allowedCharacters.Length == 0)
                throw new ArgumentException(Res.Get(Res.ArgumentEmpty), nameof(allowedCharacters));

            return GenerateString(random, random.NextInt32(minLength, maxLength, true), allowedCharacters);
        }

        public static string NextString(this Random random, int minLength = 4, int maxLength = 10, RandomString strategy = RandomString.Ascii)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            if (minLength < 0)
                throw new ArgumentOutOfRangeException(nameof(minLength), Res.Get(Res.ArgumentOutOfRange));
            if (maxLength < minLength)
                throw new ArgumentOutOfRangeException(nameof(maxLength), Res.Get(Res.ArgumentOutOfRange));
            if (!Enum<RandomString>.IsDefined(strategy))
                throw new ArgumentOutOfRangeException(nameof(strategy), Res.Get(Res.ArgumentOutOfRange));

            int length = random.NextInt32(minLength, maxLength, true);
            if (length == 0)
                return String.Empty;

            switch (strategy)
            {
                case RandomString.AnyChars:
                    return GenerateString(random, length, null);

                case RandomString.AnyValidChars:
                    return GenerateString(random, length, null, true);

                case RandomString.Ascii:
                    return GenerateString(random, length, ascii);

                case RandomString.Digits:
                    return GenerateString(random, length, digits);

                case RandomString.DigitsNoLeadingZeros:
                    return digits[random.Next(1, digits.Length)] + GenerateString(random, length - 1, digits);

                case RandomString.Letters:
                    return GenerateString(random, length, letters);

                case RandomString.LettersAndDigits:
                    return GenerateString(random, length, lettersAndDigits);

                case RandomString.UpperCaseLetters:
                    return GenerateString(random, length, upperCaseLetters);

                case RandomString.LowerCaseLetters:
                    return GenerateString(random, length, lowerCaseLetters);

                case RandomString.TitleCaseLetters:
                    return upperCaseLetters[random.Next(0, upperCaseLetters.Length)] + GenerateString(random, length - 1, lowerCaseLetters);

                case RandomString.UpperCaseWord:
                    return WordGenerator.GenerateWord(random, length).ToUpperInvariant();

                case RandomString.LowerCaseWord:
                    return WordGenerator.GenerateWord(random, length);

                case RandomString.TitleCaseWord:
                    string word = WordGenerator.GenerateWord(random, length);
                    return Char.ToUpperInvariant(word[0]) + word.Substring(1);

                case RandomString.Sentence:
                    return WordGenerator.GenerateSentence(random, length);

                default:
                    throw new InvalidOperationException("Unexpected strategy");
            }
        }

        #endregion

        // TODO: enum, object

        #region IEnumerable

        /// <summary>
        /// Shuffles an enumerable <paramref name="collection"/> (randomizes its elements).
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The <see cref="IEnumerable{T}"/> to shuffle its elements.</param>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> which contains the elements of the <paramref name="collection"/> in randomized order.</returns>
        public static IEnumerable<T> Shuffle<T>(this Random random, IEnumerable<T> collection)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            if (collection == null)
                throw new ArgumentNullException(nameof(collection), Res.Get(Res.ArgumentNull));

            return collection.Select(item => new { Index = random.Next(), Value = item }).OrderBy(i => i.Index).Select(i => i.Value);
        }

        #endregion

        #region Private Methods

#if !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static double NextDoubleLinear(Random random, double minValue, double maxValue)
        {
            double sample = random.NextDouble();
            return (maxValue * sample) + (minValue * (1d - sample));
        }

        private static decimal NextDecimalLinear(Random random, decimal minValue, decimal maxValue)
        {
            decimal sample = random.NextDecimal();
            return (maxValue * sample) + (minValue * (1m - sample));
        }

        private static string GenerateString(Random random, int length, string allowedCharacters, bool checkInvalid = false)
        {
            if (length == 0)
                return String.Empty;

            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                do
                {
                    result[i] = allowedCharacters?[random.Next(allowedCharacters.Length)] ?? random.NextChar();
                } while (checkInvalid && Char.IsSurrogate(result[i]));
            }

            return new String(result);
        }

        #endregion

        #endregion
    }
}
