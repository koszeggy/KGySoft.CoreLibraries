#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomizedPerformanceTest.cs
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
using KGySoft.Diagnostics;
using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries
{
    internal class RandomizedPerformanceTest : PerformanceTestBase<Action<Random>, object>
    {
        #region Fields

        private FastRandom random;

        #endregion

        #region Properties

        public int Seed { get; set; }

        public new string TestName
        {
            get => base.TestName;
            set => base.TestName = $"{value} ({PerformanceTest.FrameworkVersion})";
        }

        #endregion

        #region Methods

        protected override object Invoke(Action<Random> del)
        {
            del.Invoke(random);
            return null;
        }

        protected override void OnInitialize()
        {
            random = new FastRandom(Seed);
#if DEBUG
            Assert.Inconclusive("Run the performance test in Release Build");
#endif
            base.OnInitialize();
            PerformanceTest.CheckTestingFramework();
        }

        protected override void OnBeforeCase() => random = new FastRandom(Seed);

        #endregion
    }

    internal class RandomizedPerformanceTest<T> : PerformanceTestBase<Func<Random, T>, T>
    {
        #region Fields

        private FastRandom random;

        #endregion

        #region Properties

        public int Seed { get; set; }

        public new string TestName
        {
            get => base.TestName;
            set => base.TestName = $"{value} ({PerformanceTest.FrameworkVersion})";
        }

        #endregion

        #region Methods

        protected override T Invoke(Func<Random, T> del) => del.Invoke(random);

        protected override void OnInitialize()
        {
            random = new FastRandom(Seed);
#if DEBUG
            Assert.Inconclusive("Run the performance test in Release Build");
#endif
            base.OnInitialize();
            PerformanceTest.CheckTestingFramework();
        }

        protected override void OnBeforeCase() => random = new FastRandom(Seed);

        #endregion
    }
}
