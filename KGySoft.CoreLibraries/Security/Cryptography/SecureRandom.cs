﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SecureRandom.cs
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
using System.Runtime.InteropServices;
#if NETSTANDARD2_1
using System.Runtime.InteropServices; 
#endif
using System.Security; 
using System.Security.Cryptography;

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Security.Cryptography
{
    /// <summary>
    /// Represents a secure random number generator, which uses a <see cref="RandomNumberGenerator"/> instance to produce
    /// cryptographically secure random numbers.
    /// This class is functionally compatible with the <see cref="Random"/> and <see cref="FastRandom"/> classes.
    /// </summary>
    /// <remarks>
    /// <note>Please note that <see cref="SecureRandom"/> class implements the <see cref="IDisposable"/> interface
    /// so make sure you dispose it (or use it in a <see langword="using"/>&#160;block) if not used in a static context.</note>
    /// </remarks>
    /// <seealso cref="FastRandom"/>
    public class SecureRandom : Random, IDisposable
    {
        #region Constants

        private const double normalizationFactor = 1d / (1L << 53);

        #endregion

        #region Fields

        private readonly RandomNumberGenerator provider;

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureRandom"/> class.
        /// </summary>
        /// <param name="provider">A <see cref="RandomNumberGenerator"/> instance to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="provider"/> is <see langword="null"/>.</exception>
        public SecureRandom(RandomNumberGenerator provider)
        {
            if (provider == null!)
                Throw.ArgumentNullException(Argument.provider);
            this.provider = provider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureRandom"/> class.
        /// To generate cryptographically secure random numbers, a default <see cref="RandomNumberGenerator"/> will be used internally.
        /// </summary>
        public SecureRandom()
#if NETFRAMEWORK
            : this(new RNGCryptoServiceProvider())
#else
            : this(RandomNumberGenerator.Create())
#endif
        {
        }

        #region Public Methods

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">An array of bytes to contain random numbers.</param>
        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null!)
                Throw.ArgumentNullException(Argument.buffer);
            provider.GetBytes(buffer);
        }

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Fills the elements of the specified <paramref name="buffer"/> with random numbers.
        /// </summary>
        /// <param name="buffer">A <see cref="Span{T}"/> of bytes to contain random numbers.</param>
        public override void NextBytes(Span<byte> buffer) => provider.GetBytes(buffer);
#endif

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        public override double NextDouble() => SampleDouble();

#if NET6_0_OR_GREATER
        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>A single-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        public override float NextSingle() => (float)SampleDouble();
#endif

        /// <summary>
        /// Returns a non-negative random 32-bit integer that is less than <see cref="Int32.MaxValue">Int32.MaxValue</see>.
        /// </summary>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than <see cref="Int32.MaxValue">Int32.MaxValue</see>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override int Next()
        {
            int result;
            do
            {
                result = (int)SampleUInt32() & Int32.MaxValue;
            } while (result == Int32.MaxValue);

            return result;
        }

        /// <summary>
        /// Returns a non-negative random integer that is less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue" /> must be greater than or equal to 0.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue" />;
        /// that is, the range of return values ordinarily includes 0 but not <paramref name="maxValue" />.
        /// However, if <paramref name="maxValue" /> equals 0, <paramref name="maxValue" /> is returned.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override int Next(int maxValue)
        {
            if (maxValue > 1)
                return (int)(SampleUInt32() % (uint)maxValue);

            if (maxValue < 0)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            return 0;
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

            return (int)(SampleUInt32() % range) + minValue;
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Returns a non-negative random 64-bit integer that is less than <see cref="Int64.MaxValue">Int64.MaxValue</see>.
        /// </summary>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0 and less than <see cref="Int64.MaxValue">Int64.MaxValue</see>.</returns>
        /// <remarks>
        /// <para>Unlike the <see cref="RandomExtensions.NextInt64(Random)">RandomExtensions.NextInt64(Random)</see> extension method, this one does not return negative values
        /// and the result is always less than <see cref="Int64.MaxValue">Int64.MaxValue</see>.
        /// You can use the <see cref="RandomExtensions.NextInt64(Random)">RandomExtensions.NextInt64(Random)</see> method to return any <see cref="long"/> value.</para>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override long NextInt64()
        {
            long result;
            do
            {
                result = (long)SampleUInt64() & Int64.MaxValue;
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
        public override long NextInt64(long maxValue)
        {
            // using the slower 64-bit modulo division only for big range
            if (maxValue > UInt32.MaxValue)
                return maxValue == Int64.MaxValue
                    ? NextInt64()
                    : (long)(SampleUInt64() % (ulong)maxValue);
            if (maxValue > 1L)
                return (uint)SampleUInt64() % (uint)maxValue;

            if (maxValue < 0L)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo(0L));
            return 0L;
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
        public override long NextInt64(long minValue, long maxValue)
        {
            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            ulong range = (ulong)(maxValue - minValue);

            // using the slower 64-bit modulo division only for big range
            if (range > UInt32.MaxValue)
                return (long)(SampleUInt64() % range) + minValue;
            if (range > 1UL)
                return ((uint)SampleUInt64() % (uint)range) + minValue;

            return minValue;
        }
#endif

        /// <summary>
        /// Disposes the current <see cref="SecureRandom"/> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Returns a random floating-point number between 0.0 and 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        protected override double Sample() => SampleDouble();

        /// <summary>
        /// Releases the resources used by this <see cref="SecureRandom"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/>&#160;if this method is being called due to a call to <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
#if !NET35
            if (disposing)
                provider.Dispose();
#endif
        }

        #endregion

        #region Private Methods

        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private unsafe uint SampleUInt32()
        {
#if NETCOREAPP3_0_OR_GREATER
            Span<byte> bytes = stackalloc byte[4];
            provider.GetBytes(bytes);
            return Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(bytes)); 
#elif NETSTANDARD2_1_OR_GREATER
            Span<byte> bytes = stackalloc byte[4];
            provider.GetBytes(bytes);
            fixed (byte* p = bytes)
                return *(uint*)p;
#else
            byte[] bytes = new byte[4];
            provider.GetBytes(bytes);
            fixed (byte* p = bytes)
                return *(uint*)p;
#endif
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private unsafe ulong SampleUInt64()
        {
#if NETCOREAPP3_0_OR_GREATER
            Span<byte> bytes = stackalloc byte[8];
            provider.GetBytes(bytes);
            return Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(bytes));
#elif NETSTANDARD2_1_OR_GREATER
            Span<byte> bytes = stackalloc byte[8];
            provider.GetBytes(bytes);
            fixed (byte* p = bytes)
                return *(ulong*)p;
#else
            byte[] bytes = new byte[8];
            provider.GetBytes(bytes);
            fixed (byte* p = bytes)
                return *(ulong*)p;
#endif
        }

        private double SampleDouble()
            // double has 53 significant bits and 11 bit exponent so using a [0..2^53) sample
            // (see https://en.wikipedia.org/wiki/Double-precision_floating-point_format) 
            => (SampleUInt64() >> 11) * normalizationFactor;

        #endregion

        #endregion
    }
}
