#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ProfilerTest.cs
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
using System.Linq;

using KGySoft.Diagnostics;
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Diagnostics
{
    [TestFixture]
    public class ProfilerTest
    {
        #region Methods

        #region Public Methods

        [Test]
        public void MesureTest()
        {
            const string category = "ProfilerTest.MeasureTest";

            Profiler.Enabled = true;
            Profiler.AutoSaveResults = false;
            const int iteration = 10000;
            using (Profiler.Measure(category, "FullLength"))
            {
                MethodAccessor mi = MethodAccessor.GetAccessor(((Func<int>)TestMethod).Method);
                for (int i = 0; i < iteration; i++)
                {
                    using (Profiler.Measure(category, "InvokeMethod"))
                    {
                        mi.Invoke(this);
                    }
                }
            }

            Assert.AreEqual(2, Profiler.GetMeasurementResults(category).Count());
            var full = Profiler.GetMeasurementResult(category, "FullLength");
            var invoke = Profiler.GetMeasurementResult(category, "InvokeMethod");
            Assert.AreEqual(1, full.NumberOfCalls);
            Assert.AreEqual(iteration, invoke.NumberOfCalls);
            Assert.IsTrue(full.TotalTime >= invoke.TotalTime);
        }

        #endregion

        #region Private Methods

        private int TestMethod() => 0;

        #endregion

        #endregion
    }
}
