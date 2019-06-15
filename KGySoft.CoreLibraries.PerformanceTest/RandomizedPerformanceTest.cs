#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomizedPerformanceTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
using KGySoft.Diagnostics;
using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries
{
    internal class RandomizedPerformanceTest : PerformanceTestBase<Action<Random>, object>
    {
        #region Fields

        private Random random;

        #endregion

        #region Properties

        public int Seed { get; set; }

        #endregion

        #region Methods

        protected override object Invoke(Action<Random> del)
        {
            del.Invoke(random);
            return null;
        }

        protected override void OnInitialize()
        {
            random = new Random(Seed);
#if DEBUG
            Assert.Inconclusive("Run the performance test in Release Build");
#endif
            base.OnInitialize();
            PerformanceTest.CheckTestingFramework();
        }

        protected override void OnBeforeCase() => random = new Random(Seed);

        #endregion
    }

    internal class RandomizedPerformanceTest<T> : PerformanceTestBase<Func<Random, T>, T>
    {
        #region Fields

        private Random random;

        #endregion

        #region Properties

        public int Seed { get; set; }

        #endregion

        #region Methods

        protected override T Invoke(Func<Random, T> del) => del.Invoke(random);

        protected override void OnInitialize()
        {
            random = new Random(Seed);
#if DEBUG
            Assert.Inconclusive("Run the performance test in Release Build");
#endif
            base.OnInitialize();
            PerformanceTest.CheckTestingFramework();
        }

        protected override void OnBeforeCase() => random = new Random(Seed);

        #endregion
    }
}
