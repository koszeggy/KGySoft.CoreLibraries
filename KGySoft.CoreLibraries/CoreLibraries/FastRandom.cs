#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastRandom.cs
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
using System.Runtime.CompilerServices;
using System.Security;

using KGySoft.Security.Cryptography;

#endregion

#region Suppressions

#if !NET6_0_OR_GREATER
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    // This class implements the 128-bit XorShift+ algorithm. See https://en.wikipedia.org/wiki/Xorshift#xorshift+
    /// <summary>
    /// Represents a pseudo random number generator, which is functionally compatible
    /// with the <see cref="Random"/> class but is significantly faster than that.
    /// For cryptographically secure random numbers use the <see cref="SecureRandom"/> class instead.
    /// </summary>
    public sealed class FastRandom : Random
    {
        #region Nested structs

        private struct UInt128
        {
            #region Fields

            internal ulong A, B;

            #endregion

            #region Methods

            public override string ToString() => $"A={A:X16};B={B:X16}";

            #endregion
        }

        #endregion

        #region Constants

        private const double normalizationFactor = 1d / (1L << 53);

        #endregion

        #region Fields

        private UInt128 state;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FastRandom"/> class using a random seed value.
        /// </summary>
        public FastRandom() : this(Guid.NewGuid())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeRandom"/> class using the specified <paramref name="seed"/> value.
        /// </summary>
        /// <param name="seed">A number used to calculate a starting value for the pseudo-random number sequence.</param>
        public FastRandom(int seed) => state = new UInt128
        {
            // Trying to scatter the seed value so even close seeds generate very different sequences.
            A = (ulong)~seed * 13 << 32 | (uint)seed * 397,
            B = ((ulong)seed * 13 << 32 | (uint)~seed * 397) ^ 0xAAAA_AAAA_AAAA_AAAA
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeRandom"/> class using the specified <paramref name="seed"/> value.
        /// </summary>
        /// <param name="seed">A number used to calculate a starting value for the pseudo-random number sequence.</param>
        /// <exception cref="ArgumentException"><paramref name="seed"/> is <see cref="Guid.Empty"/>.</exception>
        [SecuritySafeCritical]
        public unsafe FastRandom(Guid seed)
        {
            if (seed == Guid.Empty)
                Throw.ArgumentException(Argument.seed, Res.ArgumentEmpty);
            state = *(UInt128*)&seed;
        }

        #endregion

        #region Methods

        #region Static Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static ulong SampleUInt64(ref UInt128 state)
        {
            // this is the C# version of the XorShift+ algorithm from here: https://en.wikipedia.org/wiki/Xorshift#xorshift+
            ulong t = state.A;
            ulong s = state.B;
            t ^= t << 23;
            t ^= t >> 17;
            t ^= s ^ (s >> 26);
            state.A = s;
            state.B = t;
            return t + s;
        }

        #endregion

        #region Instance Methods

        #region Public Methods

        #region Int32

        /// <summary>
        /// Returns a random <see cref="int"/> can have any value.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <returns>A 32-bit signed integer that is greater than or equal to <see cref="Int32.MinValue">Int32.MinValue</see> and less or equal to <see cref="Int32.MaxValue">Int32.MaxValue</see>.</returns>
        /// <remarks>
        /// <para>Similarly to the <see cref="Next()">Next</see> and <see cref="NextInt32">NextInt32</see> methods this one returns an <see cref="int"/> value; however, the result can be negative and
        /// the maximum possible value can be <see cref="Int32.MaxValue">Int32.MaxValue</see>.</para>
        /// <para>The <see cref="RandomExtensions.SampleInt32(Random)">RandomExtensions.SampleInt32(Random)</see> extension method has the same functionality
        /// but it is faster to call this one directly.</para>
        /// </remarks>
        public int SampleInt32() => (int)SampleUInt64(ref state);

        /// <summary>
        /// Returns a non-negative random 32-bit integer that is less than <see cref="Int32.MaxValue">Int32.MaxValue</see>.
        /// </summary>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than <see cref="Int32.MaxValue">Int32.MaxValue</see>.</returns>
        /// <remarks>
        /// <note type="caution">Starting with version 6.0.0 the behavior of this method has been changed to be conform with the behavior
        /// of the <see cref="Random.NextInt64()">Random.NextInt64</see> method introduced in .NET 6.0 so it returns the same range as the <see cref="Next()"/> method.
        /// Use the <see cref="SampleInt32">SampleInt32</see> method to obtain any <see cref="int"/> value.</note>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int NextInt32()
        {
            int result;
            do
            {
                // we could use a SampleUInt32() method, which generates a 64-bit sample for every second time
                // but actually that is slower on 64-bit builds both in .NET Framework and .NET Core.
                result = (int)SampleUInt64(ref state) & Int32.MaxValue;
            } while (result == Int32.MaxValue);

            return result;
        }

        /// <summary>
        /// Returns a non-negative random 32-bit integer that is less than <see cref="Int32.MaxValue">Int32.MaxValue</see>.
        /// </summary>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than <see cref="Int32.MaxValue">Int32.MaxValue</see>. </returns>
        /// <remarks>
        /// <note>This method just calls the <see cref="NextInt32">NextInt32</see> method.
        /// You can call directly the non-virtual <see cref="NextInt32">NextInt32</see> method for a slightly better performance.</note>
        /// </remarks>
        public override int Next() => NextInt32();

        /// <summary>
        /// Returns a non-negative random 32-bit integer that is less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue" /> must be greater than or equal to 0.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue" />;
        /// that is, the range of return values ordinarily includes 0 but not <paramref name="maxValue" />.
        /// However, if <paramref name="maxValue" /> equals 0, <paramref name="maxValue" /> is returned.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override int Next(int maxValue)
        {
            if (maxValue <= 1)
            {
                if (maxValue < 0)
                    Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
                return 0;
            }

            uint mask = ((uint)maxValue).GetBitMask();
            uint result;
            do
                result = (uint)SampleUInt64(ref state) & mask;
            while (result >= (uint)maxValue);

            return (int)result;
        }

        /// <summary>
        /// Returns a random 32-bit integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue" /> must be greater than or equal to <paramref name="minValue" />.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue" />;
        /// that is, the range of return values ordinarily includes 0 but not <paramref name="maxValue" />.
        /// However, if <paramref name="maxValue" /> equals 0, <paramref name="maxValue" /> is returned.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override int Next(int minValue, int maxValue)
        {
            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            uint range = (uint)(maxValue - minValue);
            if (range <= 1u)
                return minValue;

            uint mask = range.GetBitMask();
            uint result;
            do
                result = (uint)SampleUInt64(ref state) & mask;
            while (result >= range);

            return (int)result + minValue;
        }

        #endregion

        #region Int64

        /// <summary>
        /// Returns a random <see cref="long"/> can have any value.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <returns>A 64-bit signed integer that is greater than or equal to <see cref="Int64.MinValue">Int64.MinValue</see> and less or equal to <see cref="Int64.MaxValue">Int64.MaxValue</see>.</returns>
        /// <remarks>
        /// <para>Similarly to the <see cref="NextInt64()">NextInt64</see> method this one returns an <see cref="long"/> value; however, the result can be negative and
        /// the maximum possible value can be <see cref="Int64.MaxValue">Int64.MaxValue</see>.</para>
        /// <para>The <see cref="RandomExtensions.SampleInt64(Random)">RandomExtensions.SampleInt64(Random)</see> extension method has the same functionality
        /// but it is faster to call this one directly.</para>
        /// </remarks>
        public long SampleInt64() => (long)SampleUInt64(ref state);

        /// <summary>
        /// Returns a non-negative random 64-bit integer that is less than <see cref="Int64.MaxValue">Int64.MaxValue</see>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0 and less than <see cref="Int64.MaxValue">Int64.MaxValue</see>.</returns>
        /// <remarks>
        /// <note type="caution">Starting with version 6.0.0 the behavior of this method has been changed to be conform with the behavior
        /// of the <see cref="Random.NextInt64()">Random.NextInt64</see> method introduced in .NET 6.0 so it returns the same range.
        /// Use the <see cref="SampleInt64">SampleInt64</see> method to obtain any <see cref="long"/> value.</note>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
#if NET6_0_OR_GREATER
        override
#endif
        public long NextInt64()
        {
            long result;
            do
            {
                result = SampleInt64() & Int64.MaxValue;
            } while (result == Int64.MaxValue);

            return result;
        }

        /// <summary>
        /// Returns a non-negative random 64-bit integer that is less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue" /> must be greater than or equal to 0.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue" />;
        /// that is, the range of return values ordinarily includes 0 but not <paramref name="maxValue" />.
        /// However, if <paramref name="maxValue" /> equals 0, <paramref name="maxValue" /> is returned.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
#if NET6_0_OR_GREATER
        override
#endif
        public long NextInt64(long maxValue)
        {
            if (maxValue <= 1L)
            {
                if (maxValue < 0L)
                    Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo(0L));
                return 0L;
            }

            ulong mask = ((ulong)maxValue).GetBitMask();
            ulong result;
            do
                result = SampleUInt64(ref state) & mask;
            while (result >= (ulong)maxValue);

            return (long)result;
        }

        /// <summary>
        /// Returns a random 64-bit integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue" /> must be greater than or equal to <paramref name="minValue" />.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue" />;
        /// that is, the range of return values ordinarily includes 0 but not <paramref name="maxValue" />.
        /// However, if <paramref name="maxValue" /> equals 0, <paramref name="maxValue" /> is returned.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
#if NET6_0_OR_GREATER
        override
#endif
        public long NextInt64(long minValue, long maxValue)
        {
            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            ulong range = (ulong)(maxValue - minValue);
            if (range <= 1UL)
                return minValue;

            ulong mask = range.GetBitMask();
            ulong result;
            do
                result = SampleUInt64(ref state) & mask;
            while (result >= range);

            return (long)result + minValue;
        }

        #endregion

        #region Float

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        public override double NextDouble() => SampleDouble();

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>A single-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
#if NET6_0_OR_GREATER
        override
#endif
        public float NextSingle() => (float)SampleDouble();

        #endregion

        #region Buffer

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">An array of bytes to contain random numbers.</param>
        [SecuritySafeCritical]
        public override unsafe void NextBytes(byte[] buffer)
        {
            if (buffer == null!)
                Throw.ArgumentNullException(Argument.buffer);

            fixed (byte* pBuf = buffer)
                FillBytes(pBuf, buffer.Length);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Fills the elements of the specified <paramref name="buffer"/> with random numbers.
        /// </summary>
        /// <param name="buffer">A <see cref="Span{T}"/> of bytes to contain random numbers.</param>
        [SecuritySafeCritical]
        public override unsafe void NextBytes(Span<byte> buffer)
        {
            if (buffer.Length == 0)
                return;

            fixed (byte* pBuf = buffer)
                FillBytes(pBuf, buffer.Length);
        }
#endif

        #endregion

        #region Unsigned

        /// <summary>
        /// Returns a random 32-bit unsigned integer that is less than <see cref="UInt32.MaxValue">UInt32.MaxValue</see>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <returns>A 32-bit unsigned integer that is greater than or equal to 0 and less than <see cref="UInt32.MaxValue">UInt32.MaxValue</see>.</returns>
        /// <remarks>
        /// <note type="caution">Starting with version 6.0.0 the behavior of this method has been changed to be conform with the behavior of the <see cref="NextInt64()"/>
        /// method introduced in .NET 6.0. Cast the result of the <see cref="SampleInt32">SampleInt32</see> method to obtain any <see cref="uint"/> value.</note>
        /// <para>Unlike the <see cref="RandomExtensions.NextUInt32(Random)">RandomExtensions.NextUInt32(Random)</see> extension method, this one always returns
        /// values less than <see cref="UInt32.MaxValue">UInt32.MaxValue</see>.</para>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [CLSCompliant(false)]
        public uint NextUInt32()
        {
            uint result;
            do
            {
                result = (uint)SampleUInt64(ref state);
            } while (result == UInt32.MaxValue);

            return result;
        }

        /// <summary>
        /// Returns a random 64-bit unsigned integer that is less than <see cref="UInt64.MaxValue">UInt64.MaxValue</see>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <returns>A 64-bit unsigned integer that is greater than or equal to 0 and less than <see cref="UInt64.MaxValue">UInt64.MaxValue</see>.</returns>
        /// <remarks>
        /// <note type="caution">Starting with version 6.0.0 the behavior of this method has been changed to be conform with the behavior of the <see cref="NextInt64()"/>
        /// method introduced in .NET 6.0. Cast the result of the <see cref="SampleInt64">SampleInt64</see> method to obtain any <see cref="ulong"/> value.</note>
        /// <para>Unlike the <see cref="RandomExtensions.NextUInt64(Random)">RandomExtensions.NextUInt64(Random)</see> extension method, this one always returns
        /// values less than <see cref="UInt64.MaxValue">UInt64.MaxValue</see>.</para>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [CLSCompliant(false)]
        public ulong NextUInt64()
        {
            ulong result;
            do
            {
                result = SampleUInt64(ref state);
            } while (result == UInt64.MaxValue);

            return result;
        }

        #endregion

        #endregion

        #region Protected Methods

        /// <summary>
        /// Returns a random floating-point number between 0.0 and 1.0.
        /// </summary>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        protected override double Sample() => SampleDouble();

        #endregion

        #region Private Methods

        private double SampleDouble()
            // double has 53 significant bits and 11 bit exponent so using a [0..2^53) sample
            // (see https://en.wikipedia.org/wiki/Double-precision_floating-point_format) 
            => (SampleUInt64(ref state) >> 11) * normalizationFactor;

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private unsafe void FillBytes(byte* pBuf, int bufLen)
        {
            UInt128 stateLocal = state;

            // filling up the buffer with 64-bit chunks as long as possible
            int len = bufLen >> 3;
            ulong* pQWord = (ulong*)pBuf;
            for (int i = 0; i < len; i++)
                pQWord[i] = SampleUInt64(ref stateLocal);

            byte* pByte = (byte*)(pQWord + len);
            len = bufLen & 7;

            if (len != 0)
            {
                // filling up the rest of the bytes (up to 7 bytes)
                ulong finalSample = SampleUInt64(ref stateLocal);

                // 32 bit at once if possible
                if (len >= 4)
                {
                    *(uint*)pByte = (uint)finalSample;
                    len -= 4;
                    if (len != 0)
                    {
                        finalSample >>= 32;
                        pByte += 4;
                    }
                }

                // last bytes (up to 3)
                for (int i = 0; i < len; i++, finalSample >>= 8)
                    pByte[i] = (byte)finalSample;
            }

            state = stateLocal;
        }

        #endregion

        #endregion

        #endregion
    }
}
