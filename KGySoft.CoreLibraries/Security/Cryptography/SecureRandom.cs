﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SecureRandom.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using System.Security; 
using System.Security.Cryptography;

using KGySoft.CoreLibraries;

#endregion

#region Suppressions

#if !NET6_0_OR_GREATER
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.Security.Cryptography
{
    /// <summary>
    /// Represents a secure random number generator, which uses a <see cref="RandomNumberGenerator"/> instance to produce
    /// cryptographically secure random numbers. You can use the static <see cref="Instance"/> property to use a thread safe shared instance.
    /// This class is functionally compatible with the <see cref="Random"/> and <see cref="FastRandom"/> classes so you can use all the extensions
    /// for it that are exposed by the <see cref="RandomExtensions"/> class.
    /// </summary>
    /// <remarks>
    /// <note>Please note that <see cref="SecureRandom"/> class implements the <see cref="IDisposable"/> interface. If you use a custom instance
    /// instead of the static <see cref="Instance"/> property make sure you dispose it if it's not used anymore.</note>
    /// </remarks>
    /// <seealso cref="FastRandom"/>
    public class SecureRandom : Random, IDisposable
    {
        #region SecureRandomInstance class

        /// <summary>
        /// The singleton instance for the Instance property.
        /// It can be this simple because RNGCryptoServiceProvider.GetBytes is thread safe according to the documentation
        /// and also the RandomNumberGenerator.Create instance returns a singleton instance.
        /// </summary>
        private sealed class SecureRandomInstance : SecureRandom
        {
            #region Methods

            #region Public Methods
            // Just some overrides to provide a slightly better performance by the static members of RandomNumberGenerator where available.
            // Otherwise, the base implementation will use virtual method calls on the provider instance.

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            public override void NextBytes(Span<byte> buffer) => RandomNumberGenerator.Fill(buffer);

            public override void NextBytes(byte[] buffer)
            {
                if (buffer == null!)
                    Throw.ArgumentNullException(Argument.buffer);
                RandomNumberGenerator.Fill(buffer);
            }
#endif

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            public override int Next() => RandomNumberGenerator.GetInt32(Int32.MaxValue);
            public override int Next(int maxValue) => RandomNumberGenerator.GetInt32(maxValue);
            public override int Next(int minValue, int maxValue) => RandomNumberGenerator.GetInt32(minValue, maxValue);
#endif

            #endregion

            #region Protected Methods

            protected override void Dispose(bool disposing)
            {
                // not calling base so the provider field will not be accidentally disposed.
            }

            #endregion

            #endregion
        }

        #endregion

        #region Constants

#if NET6_0_OR_GREATER
        private const float normalizationFactorFloat = 1f / (1L << 24);
#endif
        private const double normalizationFactorDouble = 1d / (1L << 53);

        #endregion

        #region Fields

        private readonly RandomNumberGenerator provider;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a <see cref="SecureRandom"/> instance that can be used from any threads concurrently.
        /// </summary>
        /// <remarks>
        /// <note type="caution">Starting with .NET 6.0 the base <see cref="Random"/> class has a <see cref="Random.Shared"/> property, which is accessible
        /// also from the <see cref="SecureRandom"/> class, though it returns a <see cref="Random"/> instance, which is not cryptographically secure.</note>
        /// </remarks>
        public static SecureRandom Instance { get; } = new SecureRandomInstance();

        #endregion

        #region Constructors

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

        #endregion

        #region Methods

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

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
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
        public override float NextSingle() => SampleSingle();
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
            if (maxValue <= 1)
            {
                if (maxValue < 0)
                    Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
                return 0;
            }

            uint mask = ((uint)maxValue).GetBitMask();
            uint result;
            do
                result = SampleUInt32() & mask;
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
                result = SampleUInt32() & mask;
            while (result >= range);

            return (int)result + minValue;
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Returns a non-negative random 64-bit integer that is less than <see cref="Int64.MaxValue">Int64.MaxValue</see>.
        /// </summary>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0 and less than <see cref="Int64.MaxValue">Int64.MaxValue</see>.</returns>
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
            if (maxValue <= 1L)
            {
                if (maxValue < 0L)
                    Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo(0L));
                return 0L;
            }

            ulong mask = ((ulong)maxValue).GetBitMask();
            ulong result;
            do
                result = SampleUInt64() & mask;
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
        public override long NextInt64(long minValue, long maxValue)
        {
            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            ulong range = (ulong)(maxValue - minValue);
            if (range <= 1UL)
                return minValue;

            ulong mask = range.GetBitMask();
            ulong result;
            do
                result = SampleUInt64() & mask;
            while (result >= range);

            return (long)result + minValue;
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
        /// <param name="disposing"><see langword="true"/> if this method is being called due to a call to <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
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
#elif NETCOREAPP2_1 || NETSTANDARD2_1_OR_GREATER
            Span<byte> bytes = stackalloc byte[4];
            provider.GetBytes(bytes);
            fixed (byte* p = bytes)
                return *(uint*)p;
#else
            byte[] bytes = new byte[4];
            provider.GetBytes(bytes);
#if NETFRAMEWORK || NETSTANDARD2_0
            if (EnvironmentHelper.IsPartiallyTrustedDomain)
                return BitConverter.ToUInt32(bytes, 0);
#endif
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
#elif NETCOREAPP2_1 || NETSTANDARD2_1_OR_GREATER
            Span<byte> bytes = stackalloc byte[8];
            provider.GetBytes(bytes);
            fixed (byte* p = bytes)
                return *(ulong*)p;
#else
            byte[] bytes = new byte[8];
            provider.GetBytes(bytes);
#if NETFRAMEWORK || NETSTANDARD2_0
            if (EnvironmentHelper.IsPartiallyTrustedDomain)
                return BitConverter.ToUInt64(bytes, 0);
#endif
            fixed (byte* p = bytes)
                return *(ulong*)p;
#endif
        }

        private double SampleDouble()
            // double has 53 significant bits so using a [0..2^53) sample (64 - 11 bits)
            // (see https://en.wikipedia.org/wiki/Double-precision_floating-point_format)
            => (SampleUInt64() >> 11) * normalizationFactorDouble;

#if NET6_0_OR_GREATER
        private float SampleSingle()
            // float has 24 significant bits so using a [0..2^24) sample (32 - 8 bits)
            // (see https://en.wikipedia.org/wiki/Single-precision_floating-point_format)
            => (SampleUInt32() >> 8) * normalizationFactorFloat;
#endif

        #endregion

        #endregion
    }
}
