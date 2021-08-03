#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IteratorPerformanceTest.cs
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
    internal class IteratorPerformanceTest : PerformanceTestBase<Action<int>, object>
    {
        #region Fields

        private int i;

        #endregion

        #region Properties

        public new string TestName
        {
            get => base.TestName;
            set => base.TestName = $"{value} ({PerformanceTest.FrameworkVersion})";
        }

        #endregion

        #region Methods

        protected override object Invoke(Action<int> del)
        {
            // can occur during warm-up
            if (i == Iterations)
                i = 0;
            del.Invoke(i);
            i += 1;
            return null;
        }

        protected override void OnInitialize()
        {
            i = 0;
#if DEBUG
            Assert.Inconclusive("Run the performance test in Release Build");
#endif
            base.OnInitialize();
            PerformanceTest.CheckTestingFramework();
        }

        protected override void OnBeforeCase() => i = 0;

        #endregion
    }

    internal class IteratorPerformanceTest<T> : PerformanceTestBase<Func<int, T>, T>
    {
        #region Fields

        private int i;

        #endregion

        #region Properties

        public new string TestName
        {
            get => base.TestName;
            set => base.TestName = $"{value} ({PerformanceTest.FrameworkVersion})";
        }

        #endregion

        #region Methods

        protected override T Invoke(Func<int, T> del)
        {
            // can occur during warm-up
            if (i == Iterations)
                i = 0;
            return del.Invoke(i++);
        }

        protected override void OnInitialize()
        {
            i = 0;
#if DEBUG
            Assert.Inconclusive("Run the performance test in Release Build");
#endif
            base.OnInitialize();
            PerformanceTest.CheckTestingFramework();
        }

        protected override void OnBeforeCase() => i = 0;

        #endregion
    }
}
