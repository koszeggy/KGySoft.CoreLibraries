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
    /// You can use the static <see cref="O:KGySoft.CoreLibraries.ThreadSafeRandom.Create">Create</see> methods to create a customized instance,
    /// or just use the static <see cref="Instance"/> property for a fast shared instance.
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

            [ThreadStatic] private static FastRandom threadInstance;

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
            public override double NextDouble() => ThreadInstance.NextDouble();
            public override void NextBytes(byte[] buffer) => ThreadInstance.NextBytes(buffer);

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

            public override int Next() => threadInstance.Value.Next();
            public override int Next(int maxValue) => threadInstance.Value.Next(maxValue);
            public override int Next(int minValue, int maxValue) => threadInstance.Value.Next(minValue, maxValue);
            public override double NextDouble() => threadInstance.Value.NextDouble();
            public override void NextBytes(byte[] buffer) => threadInstance.Value.NextBytes(buffer);

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0)
            /// <summary>
            /// Fills the elements of the specified <paramref name="buffer"/> with random numbers.
            /// </summary>
            /// <param name="buffer">A <see cref="Span{T}"/> of bytes to contain random numbers.</param>
            public override void NextBytes(Span<byte> buffer) => threadInstance.Value.NextBytes(buffer);
#endif

            #endregion

            #region Protected Methods

            protected override double Sample() => threadInstance.Value.NextDouble();

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

        private static ThreadSafeRandom staticInstance;

        #endregion

        #region Instance Fields

        private readonly ThreadSafeRandom provider;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets a thread-safe <see cref="Random"/> instance initialized by a random seed value for each thread independently.
        /// </summary>
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
        [Obsolete("This member is maintained for compatibility reasons. Use the static Create method for a slightly better performance.")]
        public ThreadSafeRandom(int seed) => provider = Create(seed);

        #endregion

        #region Private Constructors

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
        /// <remarks>
        /// <para>Make sure the created instance is disposed if it is not used anymore.</para>
        /// <note>Please note that two generated sequence can be different even with the same starting <paramref name="seed"/> if the created instance is accessed from different threads.</note>
        /// </remarks>
        public static ThreadSafeRandom Create(int seed) => new ThreadSafeRandomWrapper(seed);

        /// <summary>
        /// Creates a <see cref="ThreadSafeRandom"/> instance using the specified <paramref name="factory"/> in each thread the result is accessed from.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="factory">A delegate that will be invoked once in each thread the created instance is used from.</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>Make sure the created instance is disposed if it is not used anymore even if the created instances are not disposable.</para>
        /// <para>Disposing the created instance disposes also the <see cref="Random"/> instances created by the <paramref name="factory"/> if the created <see cref="Random"/> instances are disposable.</para>
        /// <note>If <paramref name="factory"/> creates a pseudo random number generator, then in order not to produce the same sequence from the different threads make sure the <paramref name="factory"/> method creates instances with different seeds.</note>
        /// </remarks>
        public static ThreadSafeRandom Create(Func<Random> factory) => new ThreadSafeRandomWrapper(factory ?? Throw.ArgumentNullException<Func<Random>>(Argument.factory));

        #endregion

        #region Instance Methods

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

        #endregion
    }
}
