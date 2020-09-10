#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeRandom.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Threading;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a thread-safe pseudo-random number generator.
    /// Provides also a shared instance accessible via the static <see cref="Instance"/> property.
    /// </summary>
    /// <seealso cref="Random" />
    public class ThreadSafeRandom : Random, IDisposable
    {
        #region Nested Classes

        #region ThreadSafeRandomDefault class

        // for compatibility reasons it must be derived from ThreadSafeRandom so it can be exposed by the Instance property directly
        private sealed class ThreadSafeRandomDefault : ThreadSafeRandom
        {
            #region Fields

            [ThreadStatic] private static Random threadInstance;

            #endregion

            #region Properties

            private static Random ThreadInstance => threadInstance ??= new Random();

            #endregion

            #region Constructors

            internal ThreadSafeRandomDefault() : base(false)
            {
            }

            #endregion

            #region Methods

            #region Public Methods

            public override int Next(int maxValue) => ThreadInstance.Next(maxValue);
            public override int Next(int minValue, int maxValue) => ThreadInstance.Next(minValue, maxValue);
            public override int Next() => ThreadInstance.Next();
            public override void NextBytes(byte[] buffer) => ThreadInstance.NextBytes(buffer);
            public override double NextDouble() => ThreadInstance.NextDouble();

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0)
            /// <summary>
            /// Fills the elements of the specified <paramref name="buffer"/> with random numbers.
            /// </summary>
            /// <param name="buffer">A <see cref="Span{T}"/> of bytes to contain random numbers.</param>
            public override void NextBytes(Span<byte> buffer) => ThreadInstance.NextBytes(buffer);
#endif

            #endregion

            #region Protected Methods

            [SuppressMessage("Usage", "CA2215:Dispose methods should call base class dispose", Justification = "No, that's the point")]
            protected override void Dispose(bool disposing)
            {
                // not calling base because this instance is exposed as a static singleton
            }

            protected override double Sample() => ThreadInstance.NextDouble();

            #endregion

            #endregion
        }

        #endregion

        #region ThreadSafeRandomWrapper class

        private sealed class ThreadSafeRandomWrapper : Random, IDisposable
        {
            #region Fields

            private readonly ThreadLocal<Random> threadInstance;
            private readonly bool trackValues;

            private bool disposed;

            #endregion

            #region Constructors

            internal ThreadSafeRandomWrapper(int seed)
            {
                threadInstance = new ThreadLocal<Random>(() =>
                {
                    var result = new Random(seed);
                    Interlocked.Increment(ref seed);
                    return result;
                });
            }

            internal ThreadSafeRandomWrapper(Func<Random> factory)
            {
                threadInstance = new ThreadLocal<Random>(factory, true);
                trackValues = true;
            }

            #endregion

            #region Methods

            #region Public Methods

            public override int Next(int maxValue) => threadInstance.Value.Next(maxValue);
            public override int Next(int minValue, int maxValue) => threadInstance.Value.Next(minValue, maxValue);
            public override int Next() => threadInstance.Value.Next();
            public override void NextBytes(byte[] buffer) => threadInstance.Value.NextBytes(buffer);
            public override double NextDouble() => threadInstance.Value.NextDouble();

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0)
            /// <summary>
            /// Fills the elements of the specified <paramref name="buffer"/> with random numbers.
            /// </summary>
            /// <param name="buffer">A <see cref="Span{T}"/> of bytes to contain random numbers.</param>
            public override void NextBytes(Span<byte> buffer) => threadInstance.Value.NextBytes(buffer);
#endif

            public void Dispose()
            {
                if (disposed)
                    return;
                if (trackValues)
                    threadInstance.Values.ForEach(rnd => (rnd as IDisposable)?.Dispose());
                threadInstance.Dispose();
                disposed = true;
            }

            #endregion

            #region Protected Methods

            protected override double Sample() => threadInstance.Value.NextDouble();

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        #region Static Fields

        private static ThreadSafeRandom staticInstance;

        #endregion

        #region Instance Fields

        private readonly Random provider;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets a thread-safe <see cref="Random"/> instance initialized by a time-dependent default seed value.
        /// </summary>
        public static ThreadSafeRandom Instance => staticInstance ??= new ThreadSafeRandomDefault();

        #endregion

        #region Constructors

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeRandom"/> class, using a time-dependent seed value.
        /// It is practically the same as using the <see cref="Instance"/> property.
        /// </summary>
        public ThreadSafeRandom() => provider = Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeRandom"/> class using the specified <paramref name="seed"/> value.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="seed">A number used to calculate a starting value for the pseudo-random number sequence. If a negative number is specified, the absolute value of the number is used.</param>
        /// <remarks>
        /// <para>Make sure the created instance is disposed if it is not used anymore.</para>
        /// <note>Please note that two generated sequence can be different even with the same starting <paramref name="seed"/> if the created instance is accessed from different threads.</note>
        /// </remarks>
        public ThreadSafeRandom(int seed) => provider = new ThreadSafeRandomWrapper(seed);

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeRandom"/> class using the specified <paramref name="factory"/>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="factory">A delegate that will be invoked once in each thread the created instance is used.</param>
        /// <remarks>
        /// <para>Make sure the created instance is disposed if it is not used anymore even if the created instances are not disposable.</para>
        /// <para>Disposing the created instance disposes also the <see cref="Random"/> instances created by the <paramref name="factory"/> if the created <see cref="Random"/> instances are disposable.</para>
        /// <note>If <paramref name="factory"/> creates a pseudo random number generator, then in order not to produce the same sequence from the different threads make sure the <paramref name="factory"/> method creates instances with different seeds.</note>
        /// </remarks>
        public ThreadSafeRandom(Func<Random> factory) => provider = new ThreadSafeRandomWrapper(factory ?? Throw.ArgumentNullException<Func<Random>>(Argument.factory));

        #endregion

        #region Private Constructors

        private ThreadSafeRandom(bool _)
        {
            // a dummy constructor for ThreadSafeRandomTimeBased to avoid the Obsolete warning
        }

        #endregion

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Returns a non-negative random integer.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is greater than or equal to 0 and less than <see cref="Int32.MaxValue">Int32.MaxValue</see>.
        /// </returns>
        public override int Next() => provider.Next();

        /// <summary>
        /// Returns a non-negative random integer that is less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue" /> must be greater than or equal to 0.</param>
        /// <returns>
        /// A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue" />; that is, the range of return values ordinarily includes 0 but not <paramref name="maxValue" />. However, if <paramref name="maxValue" /> equals 0, <paramref name="maxValue" /> is returned.
        /// </returns>
        public override int Next(int maxValue) => provider.Next(maxValue);

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue" /> must be greater than or equal to <paramref name="minValue" />.</param>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to <paramref name="minValue" /> and less than <paramref name="maxValue" />; that is, the range of return values includes <paramref name="minValue" /> but not <paramref name="maxValue" />. If <paramref name="minValue" /> equals <paramref name="maxValue" />, <paramref name="minValue" /> is returned.
        /// </returns>
        public override int Next(int minValue, int maxValue) => provider.Next(minValue, maxValue);

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        public override double NextDouble() => provider.NextDouble();

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">An array of bytes to contain random numbers.</param>
        public override void NextBytes(byte[] buffer) => provider.NextBytes(buffer);

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0)
        /// <summary>
        /// Fills the elements of the specified <paramref name="buffer"/> with random numbers.
        /// </summary>
        /// <param name="buffer">A <see cref="Span{T}"/> of bytes to contain random numbers.</param>
        public override void NextBytes(Span<byte> buffer) => provider.NextBytes(buffer);
#endif

        /// <summary>
        /// Disposes this <see cref="ThreadSafeRandom"/> instance.
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
        protected override double Sample() => provider.NextDouble();

        /// <summary>
        /// Releases the resources used by this <see cref="ThreadSafeRandom"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/>&#160;if this method is being called due to a call to <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                (provider as IDisposable)?.Dispose();
        }

        #endregion

        #endregion
    }
}
