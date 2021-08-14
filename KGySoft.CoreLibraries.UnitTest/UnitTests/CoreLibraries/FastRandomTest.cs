#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastRandomTest.cs
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

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries
{
    [TestFixture]
    public class FastRandomTest : TestBase
    {
        #region Methods

        [Test]
        public void SeedTest()
        {
            var rnd = new FastRandom(0);
            int value = rnd.Next();

            rnd = new FastRandom(0);
            Assert.AreEqual(value, rnd.Next());
        }

        [Test]
        public void GuidSeedTest()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Throws<ArgumentException>(() => new FastRandom(Guid.Empty));

            Guid seed = Guid.NewGuid();
            var rnd = new FastRandom(seed);
            int value = rnd.Next();

            rnd = new FastRandom(seed);
            Assert.AreEqual(value, rnd.Next());
        }

        [Test]
        public void NextTest()
        {
            var rnd = new FastRandom();

            // min > max
            Throws<ArgumentOutOfRangeException>(() => rnd.Next(1, 0));

            Assert.GreaterOrEqual(rnd.Next(), 0);

            // no range
            Assert.AreEqual(1, rnd.Next(1, 1));
        }

        [TestCase(-5, 5)]
        [TestCase(Int32.MinValue / 2 - 10_000, Int32.MaxValue / 2 + 10_000)]
        public void NextRangeTest(int min, int max)
        {
            var rnd = new FastRandom();

            for (int i = 0; i < 10_000; i++)
            {
                int result = rnd.Next(min, max);
                Assert.GreaterOrEqual(result, min);
                Assert.Less(result, max);
            }
        }

        [Test]
        public void NextInt64Test()
        {
            var rnd = new FastRandom();

            // min > max
            Throws<ArgumentOutOfRangeException>(() => rnd.NextInt64(1, 0));

            Assert.GreaterOrEqual(rnd.NextInt64(), 0L);

            // no range
            Assert.AreEqual(1L, rnd.NextInt64(1, 1));
        }

        [TestCase(-5, 5)]
        [TestCase(Int64.MinValue / 2 - 10_000, Int64.MaxValue / 2 + 10_000)]
        public void NextInt64RangeTest(long min, long max)
        {
            var rnd = new FastRandom();

            for (int i = 0; i < 10_000; i++)
            {
                long result = rnd.NextInt64(min, max);
                Assert.GreaterOrEqual(result, min);
                Assert.Less(result, max);
            }
        }

        [Test]
        public void NextDoubleTest()
        {
            var rnd = new FastRandom();

            for (int i = 0; i < 10_000; i++)
            {
                double result = rnd.NextDouble();
                Assert.GreaterOrEqual(result, 0d);
                Assert.Less(result, 1d);
            }
        }

        #endregion
    }
}