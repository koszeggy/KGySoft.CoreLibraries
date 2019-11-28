#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SecureRandom.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
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
using System.Diagnostics.CodeAnalysis;
#if NETCOREAPP3_0
using System.Runtime.CompilerServices; 
#endif
#if NETSTANDARD2_1
using System.Runtime.InteropServices; 
#endif
using System.Security.Cryptography;

#endregion

namespace KGySoft.Security.Cryptography
{
    /// <summary>
    /// Represents a secure random number generator, which uses a <see cref="RandomNumberGenerator"/> instance to produce
    /// cryptographically secure random numbers.
    /// This class is functionally compatible with the <see cref="Random"/> class.
    /// </summary>
    /// <remarks>
    /// <note>Please note that <see cref="SecureRandom"/> class implements the <see cref="IDisposable"/> interface
    /// so make sure you dispose it (or use it in a <see langword="using"/>&#160;block) if not used in a static context.</note>
    /// </remarks>
    /// <seealso cref="Random" />
    public class SecureRandom : Random, IDisposable
    {
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
            if (provider == null)
                Throw.ArgumentNullException(Argument.provider);
            this.provider = provider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureRandom"/> class.
        /// To generate cryptographically secure random numbers, an <see cref="RNGCryptoServiceProvider"/> will be used internally.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Provider will be disposed by Dispose.")]
        public SecureRandom()
            : this(new RNGCryptoServiceProvider())
        {
        }

        #region Public Methods

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">An array of bytes to contain random numbers.</param>
        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
                Throw.ArgumentNullException(Argument.buffer);
            provider.GetBytes(buffer);
        }

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0)
        /// <summary>
        /// Fills the elements of the specified <paramref name="buffer"/> with random numbers.
        /// </summary>
        /// <param name="buffer">A <see cref="Span{T}"/> of bytes to contain random numbers.</param>
        public override void NextBytes(Span<byte> buffer)
        {
            provider.GetBytes(buffer);
        }
#endif

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        public override double NextDouble() => Sample();

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
                result = (int)SampleUInt32() & Int32.MaxValue;
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
        public override int Next(int maxValue) => Next(0, maxValue);

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
            if (minValue > maxValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            if (minValue == maxValue)
                return minValue;

            uint range = (uint)(maxValue - minValue);
            uint limit = UInt32.MaxValue - (UInt32.MaxValue % range);
            uint sample;
            do
            {
                sample = SampleUInt32();
            }
            while (sample > limit);

            return (int)((sample % range) + (uint)minValue);
        }

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
        protected override unsafe double Sample()
        {
            // ReSharper disable once JoinDeclarationAndInitializer - due to #if
            ulong sample;
#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
            Span<byte> bytes = stackalloc byte[8];
            provider.GetBytes(bytes);
#if NETSTANDARD2_1
            return MemoryMarshal.Read<ulong>(bytes); // Unsafe.As would be much faster but that is not available in Standard
#else
            sample = Unsafe.As<byte, ulong>(ref bytes[0]);
#endif // NETSTANDARD2_1

#else // !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
            byte[] bytes = new byte[8];
            provider.GetBytes(bytes);
            fixed (byte* p = bytes)
                sample = *(ulong*)p;
#endif
            // mantissa of the double type is at the last 53 bits
            return sample / (double)(1UL << 53);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><see langword="true"/>&#160;to release both managed and unmanaged resources; <see langword="false"/>&#160;to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
#if !NET35
            if (disposing)
                provider.Dispose();
#endif
        }

        #endregion

        #region Private Methods

        private unsafe uint SampleUInt32()
        {
#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
            Span<byte> bytes = stackalloc byte[4];
            provider.GetBytes(bytes);
#if NETSTANDARD2_1
            return MemoryMarshal.Read<uint>(bytes); // Unsafe.As would be much faster but that is not available in Standard
#else
            return Unsafe.As<byte, uint>(ref bytes[0]);
#endif // NETSTANDARD2_1
#else // !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
            byte[] bytes = new byte[4];
            provider.GetBytes(bytes);
            fixed (byte* p = bytes)
                return *(uint*)p;
#endif
        }

        #endregion

        #endregion
    }
}
