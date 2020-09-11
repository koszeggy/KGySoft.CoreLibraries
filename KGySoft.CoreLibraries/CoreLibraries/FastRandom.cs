#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastRandom.cs
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
using System.Runtime.InteropServices;
using System.Security;

#endregion

namespace KGySoft.CoreLibraries
{
    // This class implements the 128-bit XorShift+ algorithm. See https://en.wikipedia.org/wiki/Xorshift#xorshift+
    /// <summary>
    /// Represents a pseudo random number generator, which is functionally compatible
    /// with the <see cref="Random"/> class but is significantly faster than that.
    /// </summary>
    public class FastRandom : Random
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

        private const double normalizationFactor = 1d / (UInt32.MaxValue + 1d);

        #endregion

        #region Fields

        private UInt128 state;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FastRandom"/> class using a random seed value.
        /// </summary>
        [SecuritySafeCritical]
        public unsafe FastRandom()
        {
            // A new Guid is ideal as a random seed as it is a real random value and has the same size
            Guid seed = Guid.NewGuid();
            state = *(UInt128*)&seed;
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

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Returns a non-negative random integer.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is greater than or equal to 0 and less than <see cref="Int32.MaxValue">Int32.MaxValue</see>.
        /// </returns>
        public override int Next()
        {
            int result;
            do
            {
                // actually we could use a SampleUInt32() method, which generates a 64-bit sample for every second time
                // but actually that is slower on 64-bit builds both in .NET Framework and .NET Core.
                result = (int)SampleUInt64() & Int32.MaxValue;
            } while (result == Int32.MaxValue);

            return result;
        }

        /// <summary>
        /// Returns a non-negative random integer that is less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue" /> must be greater than or equal to 0.</param>
        /// <returns>
        /// A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue" />; that is, the range of return values ordinarily includes 0 but not <paramref name="maxValue" />. However, if <paramref name="maxValue" /> equals 0, <paramref name="maxValue" /> is returned.
        /// </returns>
        public override int Next(int maxValue)
        {
            if (maxValue < 0)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            return (int)(SampleDouble() * maxValue);
        }

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue" /> must be greater than or equal to <paramref name="minValue" />.</param>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to <paramref name="minValue" /> and less than <paramref name="maxValue" />; that is, the range of return values includes <paramref name="minValue" /> but not <paramref name="maxValue" />. If <paramref name="minValue" /> equals <paramref name="maxValue" />, <paramref name="minValue" /> is returned.
        /// </returns>
        public override int Next(int minValue, int maxValue)
        {
            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            uint range = (uint)(maxValue - minValue);

            if (range <= Int32.MaxValue)
                return (int)(SampleDouble() * range) + minValue;
            return (int)((long)(SampleDouble() * range) + minValue);
        }

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        public override double NextDouble() => SampleDouble();

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">An array of bytes to contain random numbers.</param>
        [SecuritySafeCritical]
        public override unsafe void NextBytes(byte[] buffer)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse - false alarm: it CAN be null
            if (buffer == null)
                Throw.ArgumentNullException(Argument.buffer);

            fixed (byte* pBuf = buffer)
                FillBytes(pBuf, buffer.Length);
        }

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0)
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

        #region Protected Methods

        /// <summary>
        /// Returns a random floating-point number between 0.0 and 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        protected override double Sample() => SampleDouble();

        #endregion

        #region Private Methods

        private ulong SampleUInt64()
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

        private double SampleDouble() => (uint)SampleUInt64() * normalizationFactor;

        [SecurityCritical]
        private unsafe void FillBytes(byte* pBuf, int bufLen)
        {
            // filling up the buffer with 64-bit chunks as long as possible
            int len = bufLen >> 3;
            ulong* pQWord = (ulong*)pBuf;
            for (int i = 0; i < len; i++)
                pQWord[i] = SampleUInt64();

            byte* pByte = (byte*)(pQWord + len);
            len = bufLen & 7;
            if (len == 0)
                return;

            // filling up the rest of the bytes one by one (up to 7 bytes)
            ulong finalSample = SampleUInt64();
            for (int i = 0; i < len; i++, finalSample >>= 8)
                pByte[i] = (byte)finalSample;
        }

        #endregion

        #endregion
    }
}
