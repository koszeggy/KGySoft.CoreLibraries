#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeRandom.cs
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

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a thread-safe pseudo-random number generator.
    /// </summary>
    /// <seealso cref="Random" />
    public class ThreadSafeRandom : Random
    {
       // NOTE: Starting with .NET 2.0 the different methods do not necessarily rely on the Sample method so we have to overload every public method.

        #region Fields

        private readonly object syncRoot = new object();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeRandom"/> class, using a time-dependent seed value.
        /// </summary>
        public ThreadSafeRandom()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeRandom"/> class using the specified <paramref name="seed"/> value.
        /// </summary>
        /// <param name="seed">A number used to calculate a starting value for the pseudo-random number sequence. If a negative number is specified, the absolute value of the number is used.</param>
        public ThreadSafeRandom(int seed) : base(seed)
        {
        }

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
            lock (syncRoot)
                return base.Next();
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
            lock (syncRoot)
                return base.Next(maxValue);
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
            lock (syncRoot)
                return base.Next(minValue, maxValue);
        }

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        public override double NextDouble()
        {
            lock (syncRoot)
                return base.NextDouble();
        }

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">An array of bytes to contain random numbers.</param>
        public override void NextBytes(byte[] buffer)
        {
            lock (syncRoot)
                base.NextBytes(buffer);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Returns a random floating-point number between 0.0 and 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        protected override double Sample()
        {
            // Just for deriving this class.
            lock (syncRoot)
                return base.Sample();
        }

        #endregion

        #endregion
    }
}
