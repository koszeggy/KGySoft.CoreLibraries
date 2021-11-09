#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeRandom.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Threading;

using KGySoft.Security.Cryptography;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a thread-safe wrapper for random number generators.
    /// You can use the static <see cref="O:KGySoft.CoreLibraries.ThreadSafeRandom.Create">Create</see> methods to create a customized instance
    /// (eg. you can wrap a <see cref="SecureRandom"/> instance to generate cryptographically safe random numbers in a thread-safe way),
    /// or just use the static <see cref="Instance"/> property for a fast shared instance (which uses <see cref="FastRandom"/> internally).
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

            [ThreadStatic]private static FastRandom? threadInstance;

            #endregion

            #region Properties

            private static FastRandom ThreadInstance => threadInstance ??= new FastRandom();

            #endregion

            #region Constructors

            internal ThreadSafeRandomDefault() : base(false)
            {
            }

            #endregion

            #region Methods

            #region Public Methods

            public override int Next() => ThreadInstance.Next();
            public override int Next(int maxValue) => ThreadInstance.Next(maxValue);
            public override int Next(int minValue, int maxValue) => ThreadInstance.Next(minValue, maxValue);
            public override long NextInt64() => ThreadInstance.NextInt64();
            public override long NextInt64(long maxValue) => ThreadInstance.NextInt64(maxValue);
            public override long NextInt64(long minValue, long maxValue) => ThreadInstance.NextInt64(minValue, maxValue);
            public override double NextDouble() => ThreadInstance.NextDouble();
            public override float NextSingle() => ThreadInstance.NextSingle();

            public override void NextBytes(byte[] buffer) => ThreadInstance.NextBytes(buffer);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            /// <summary>
            /// Fills the elements of the specified <paramref name="buffer"/> with random numbers.
            /// </summary>
            /// <param name="buffer">A <see cref="Span{T}"/> of bytes to contain random numbers.</param>
            public override void NextBytes(Span<byte> buffer) => ThreadInstance.NextBytes(buffer);
#endif

            #endregion

            #region Protected Methods

#if NET5_0_OR_GREATER
            [SuppressMessage("Usage", "CA2215:Dispose methods should call base class dispose", Justification = "No, that's the point")] 
#endif
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

        private sealed class ThreadSafeRandomWrapper : ThreadSafeRandom
        {
            #region Fields

            private readonly ThreadLocal<Random> threadInstance;
            private readonly bool trackValues;

            private volatile bool disposed;

            #endregion

            #region Constructors

            internal ThreadSafeRandomWrapper(int seed) : base(false)
            {
                threadInstance = new ThreadLocal<Random>(() =>
                {
                    var result = new FastRandom(seed);
                    Interlocked.Increment(ref seed);
                    return result;
                });
            }

            internal ThreadSafeRandomWrapper(Func<Random> factory) : base(false)
            {
                trackValues = true;
                threadInstance = new ThreadLocal<Random>(factory, trackValues);
            }

            #endregion

            #region Methods

            #region Public Methods

            public override int Next() => threadInstance.Value?.Next() ?? Throw.ObjectDisposedException<int>();
            public override int Next(int maxValue) => threadInstance.Value?.Next(maxValue) ?? Throw.ObjectDisposedException<int>();
            public override int Next(int minValue, int maxValue) => threadInstance.Value?.Next(minValue, maxValue) ?? Throw.ObjectDisposedException<int>();
            public override long NextInt64() => threadInstance.Value?.NextInt64() ?? Throw.ObjectDisposedException<int>();
            public override long NextInt64(long maxValue) => threadInstance.Value?.NextInt64(maxValue) ?? Throw.ObjectDisposedException<int>();
            public override long NextInt64(long minValue, long maxValue) => threadInstance.Value?.NextInt64(minValue, maxValue) ?? Throw.ObjectDisposedException<int>();
            public override double NextDouble() => threadInstance.Value?.NextDouble() ?? Throw.ObjectDisposedException<int>();
            public override float NextSingle() => threadInstance.Value?.NextSingle() ?? Throw.ObjectDisposedException<int>();
            public override void NextBytes(byte[] buffer)
            {
                Random? random = threadInstance.Value;
                if (random == null)
                    Throw.ObjectDisposedException();
                random.NextBytes(buffer);
            }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            /// <summary>
            /// Fills the elements of the specified <paramref name="buffer"/> with random numbers.
            /// </summary>
            /// <param name="buffer">A <see cref="Span{T}"/> of bytes to contain random numbers.</param>
            public override void NextBytes(Span<byte> buffer)
            {
                Random? random = threadInstance.Value;
                if (random == null)
                    Throw.ObjectDisposedException();
                random.NextBytes(buffer);
            }
#endif

            #endregion

            #region Protected Methods

            protected override double Sample() => threadInstance.Value?.NextDouble() ?? Throw.ObjectDisposedException<double>();

            protected override void Dispose(bool disposing)
            {
                if (disposed)
                    return;
                if (trackValues)
                    threadInstance.Values.ForEach(rnd => (rnd as IDisposable)?.Dispose());
                threadInstance.Dispose();
                base.Dispose(disposing);
                disposed = true;
            }

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        #region Static Fields

        private static ThreadSafeRandom? staticInstance;

        #endregion

        #region Instance Fields

        private readonly ThreadSafeRandom? provider;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets a <see cref="ThreadSafeRandom"/> instance that can be used from any threads concurrently.
        /// </summary>
        /// <remarks>
        /// <note>This property returns a <see cref="ThreadSafeRandom"/> instance, which generates pseudo random numbers using <see cref="FastRandom"/> internally.
        /// To produce cryptographically secure random numbers use the <see cref="Create(Func{Random})"/> method instead, and initialize it by a delegate,
        /// which returns <see cref="SecureRandom"/> instances.</note>
        /// </remarks>
        public static ThreadSafeRandom Instance => staticInstance ??= new ThreadSafeRandomDefault();

        #endregion

        #region Constructors

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeRandom"/> class, using a random seed value.
        /// It is practically the same as using the <see cref="Instance"/> property.
        /// </summary>
        [Obsolete("This member is maintained for compatibility reasons. Use directly the static Instance property for a slightly better performance.")]
        public ThreadSafeRandom() => provider = Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeRandom"/> class using the specified <paramref name="seed"/> value.
        /// It is practically the same as using the <see cref="Create(int)"/> method.
        /// </summary>
        /// <param name="seed">A number used to calculate a starting value for the pseudo-random number sequence.</param>
        [Obsolete("This member is maintained for compatibility reasons. Use the static Create method for a slightly better performance.")]
        public ThreadSafeRandom(int seed) => provider = Create(seed);

        #endregion

        #region Private Constructors

        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "It's to distinct from the default constructor")]
        private ThreadSafeRandom(bool _)
        {
            // a dummy constructor for derived types to avoid the obsolete warning and provider initialization
        }

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        /// <summary>
        /// Creates a <see cref="ThreadSafeRandom"/> instance using the specified <paramref name="seed"/> value.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="seed">A number used to calculate a starting value for the pseudo-random number sequence.</param>
        /// <returns>A <see cref="ThreadSafeRandom"/> instance using the specified <paramref name="seed"/> value.</returns>
        /// <remarks>
        /// <para>Make sure the created instance is disposed if it is not used anymore.</para>
        /// <note>Please note that two generated sequences can be different even with the same starting <paramref name="seed"/> if the created instance is accessed from different threads.</note>
        /// </remarks>
        public static ThreadSafeRandom Create(int seed) => new ThreadSafeRandomWrapper(seed);

        /// <summary>
        /// Creates a <see cref="ThreadSafeRandom"/> instance using the specified <paramref name="factory"/> in each thread the result is accessed from.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="factory">A delegate that will be invoked once in each thread the created instance is used from.</param>
        /// <returns>A <see cref="ThreadSafeRandom"/> instance using the specified <paramref name="factory"/> in each thread the result is accessed from.</returns>
        /// <remarks>
        /// <para>Make sure the created instance is disposed if it is not used anymore even if the created instances are not disposable.</para>
        /// <para>Disposing the created instance disposes also the <see cref="Random"/> instances created by the <paramref name="factory"/> if the created <see cref="Random"/> instances are disposable.</para>
        /// <note>If <paramref name="factory"/> creates a pseudo random number generator, then in order not to produce the same sequence from the different threads make sure the <paramref name="factory"/> method creates instances with different seeds.</note>
        /// </remarks>
        public static ThreadSafeRandom Create(Func<Random>? factory) => new ThreadSafeRandomWrapper(factory ?? Throw.ArgumentNullException<Func<Random>>(Argument.factory));

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Returns a non-negative random 32-bit integer that is less than <see cref="Int32.MaxValue">Int32.MaxValue</see>.
        /// </summary>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than <see cref="Int32.MaxValue">Int32.MaxValue</see>. </returns>
        public override int Next() => provider!.Next();

        /// <summary>
        /// Returns a non-negative random 32-bit integer that is less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue" /> must be greater than or equal to 0.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue" />;
        /// that is, the range of return values ordinarily includes 0 but not <paramref name="maxValue" />.
        /// However, if <paramref name="maxValue" /> equals 0, <paramref name="maxValue" /> is returned.</returns>
        public override int Next(int maxValue) => provider!.Next(maxValue);

        /// <summary>
        /// Returns a random 32-bit integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue" /> must be greater than or equal to <paramref name="minValue" />.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue" />;
        /// that is, the range of return values ordinarily includes 0 but not <paramref name="maxValue" />.
        /// However, if <paramref name="maxValue" /> equals 0, <paramref name="maxValue" /> is returned.</returns>
        public override int Next(int minValue, int maxValue) => provider!.Next(minValue, maxValue);

        /// <summary>
        /// Returns a non-negative random 64-bit integer that is less than <see cref="Int64.MaxValue">Int64.MaxValue</see>.
        /// </summary>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0 and less than <see cref="Int64.MaxValue">Int64.MaxValue</see>.</returns>
        public
#if NET6_0_OR_GREATER
            override
#else
            virtual
#endif
            long NextInt64() => provider!.NextInt64();

        /// <summary>
        /// Returns a non-negative random 64-bit integer that is less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue" /> must be greater than or equal to 0.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue" />;
        /// that is, the range of return values ordinarily includes 0 but not <paramref name="maxValue" />.
        /// However, if <paramref name="maxValue" /> equals 0, <paramref name="maxValue" /> is returned.</returns>
        public
#if NET6_0_OR_GREATER
            override
#else
            virtual
#endif
            long NextInt64(long maxValue) => provider!.NextInt64(maxValue);

        /// <summary>
        /// Returns a random 64-bit integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue" /> must be greater than or equal to <paramref name="minValue" />.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue" />;
        /// that is, the range of return values ordinarily includes 0 but not <paramref name="maxValue" />.
        /// However, if <paramref name="maxValue" /> equals 0, <paramref name="maxValue" /> is returned.</returns>
        public
#if NET6_0_OR_GREATER
            override
#else
            virtual
#endif
            long NextInt64(long minValue, long maxValue) => provider!.NextInt64(minValue, maxValue);

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        public override double NextDouble() => provider!.NextDouble();

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>
        /// A single-precision floating point number that is greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        public
#if NET6_0_OR_GREATER
            override
#else
            virtual
#endif
            float NextSingle() => provider!.NextSingle();

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">An array of bytes to contain random numbers.</param>
        public override void NextBytes(byte[] buffer) => provider!.NextBytes(buffer);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Fills the elements of the specified <paramref name="buffer"/> with random numbers.
        /// </summary>
        /// <param name="buffer">A <see cref="Span{T}"/> of bytes to contain random numbers.</param>
        public override void NextBytes(Span<byte> buffer) => provider!.NextBytes(buffer);
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
        protected override double Sample() => provider!.NextDouble();

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

        #endregion
    }
}
