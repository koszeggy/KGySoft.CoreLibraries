#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ProfilerPerformanceTest.cs
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
using System.Diagnostics;
using System.Reflection;

using KGySoft.Diagnostics;
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Diagnostics
{
    [TestFixture]
    public class ProfilerPerformanceTest
    {
        #region Methods

        #region Public Methods

        /// <summary>
        /// This test just compares the different profiling methods:
        /// - PerformanceTest solution (invoking delegates)
        /// - The Profiler's solution (enclosing into using)
        /// - Direct measurement
        /// We want to just determinate whether the delegate call or using a disposable measurement class have too large
        /// overload to test micro measurements.
        /// </summary>
        [Test]
        public void CompareProfilingWaysCheapOperation()
        {
            const int iterations = 10000000;

            // 1. Test without warm up
            var test = new PerformanceTest<int>
                {
                    TestName = "DoTest performance without warm up",
                    Iterations = iterations,
                    WarmUp = false,
                    Repeat = 5,
                }
                .AddCase(DoNothing, nameof(DoNothing));

            test.DoTest().DumpResults(Console.Out);

            // 2. Test without warm up
            test.TestName = "DoTest performance with warm up";
            test.WarmUp = true;
            test.DoTest().DumpResults(Console.Out);

            // 3. Direct test
            Console.WriteLine("===========Direct measurement test===============");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // warming up
            for (int i = 0; i < iterations; i++)
                DoNothing();

            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
                DoNothing();

            watch.Stop();
            Console.WriteLine("Total Time: {0:N2} ms", watch.Elapsed.TotalMilliseconds);
            Console.WriteLine();
            Console.WriteLine("The difference of DoTest and Direct measurement is the overhead cost of using a delegate in DoTest.");
            Console.WriteLine();

            // 4. Profiler test
            Console.WriteLine("===========Profiler test===============");
            Profiler.Reset();
            Profiler.AutoSaveResults = false;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            for (int i = 0; i < iterations; i++)
            {
                using (Profiler.Measure("ProfilerTest", "WarmingUp"))
                {
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            for (int i = 0; i < iterations; i++)
            {
                using (Profiler.Measure("ProfilerTest", "SelfCostWithoutOp"))
                {
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            using (Profiler.Measure("ProfilerTest", "TotalWithSubMeasures"))
            {
                for (int i = 0; i < iterations; i++)
                {
                    using (Profiler.Measure("ProfilerTest", "DoNothingCall"))
                        DoNothing();
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            using (Profiler.Measure("ProfilerTest", "PureTotal"))
            {
                for (int i = 0; i < iterations; i++)
                    DoNothing();
            }

            foreach (IMeasureItem item in Profiler.GetMeasurementResults("ProfilerTest"))
            {
                Console.WriteLine("{0}: {1:N2} ms", item.Operation, item.TotalTime.TotalMilliseconds);
            }

            Console.WriteLine();
            Console.WriteLine("PureTotal should be nearly the same as Direct measurement, and PureTotal should be DoNothingCall - SelfCostWithoutOp");
            Console.WriteLine("TotalWithSubMeasures - DoNothingCall = the cost of the Profiler itself (SelfCostWithoutOp does not contain the administration costs of the results)");
        }

        /// <summary>
        /// Compares the profiling ways of an expensive operation.
        /// There should be no significant difference between the different ways.
        /// </summary>
        [Test]
        public void CompareProfilingWaysExpensiveOperation()
        {
            const int iterations = 10000;
            var test = new PerformanceTest<int>
                {
                    TestName = "DoTest performance without warm up",
                    Iterations = iterations,
                    WarmUp = false,
                    Repeat = 5,
                }
                .AddCase(DoSomething, nameof(DoSomething));

            // 1. DoTest without warmup
            test.DoTest().DumpResults(Console.Out);

            test.TestName = "DoTest performance with warmup";
            test.WarmUp = true;

            // 2. DoTest with warmup
            test.DoTest().DumpResults(Console.Out);

            // 3. Direct test
            Console.WriteLine("===========Direct measurement test===============");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // warming up
            for (int i = 0; i < iterations; i++)
                DoSomething();

            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
                DoSomething();

            watch.Stop();
            Console.WriteLine("Total Time: {0:N2} ms", watch.Elapsed.TotalMilliseconds);
            Console.WriteLine();

            // 4. Profiler test
            Console.WriteLine("===========Profiler test===============");
            Profiler.AutoSaveResults = false;
            Profiler.Reset();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            for (int i = 0; i < iterations; i++)
            {
                using (Profiler.Measure("ProfilerTest", "WarmingUp"))
                {
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            for (int i = 0; i < iterations; i++)
            {
                using (Profiler.Measure("ProfilerTest", "SelfCostWithoutOp"))
                {
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            using (Profiler.Measure("ProfilerTest", "TotalWithSubMeasures"))
            {
                for (int i = 0; i < iterations; i++)
                {
                    using (Profiler.Measure("ProfilerTest", "DoSomethingCall"))
                        DoSomething();
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            using (Profiler.Measure("ProfilerTest", "PureTotal"))
            {
                for (int i = 0; i < iterations; i++)
                    DoSomething();
            }

            foreach (IMeasureItem item in Profiler.GetMeasurementResults("ProfilerTest"))
            {
                Console.WriteLine("{0}: {1:N2} ms", item.Operation, item.TotalTime.TotalMilliseconds);
            }

            Console.WriteLine();
            Console.WriteLine("In case of a costly operation the DirectTotal < PureTotal < DoTest < DoSomethingCall < TotalWithSubMeasures should have nearly the same value.");
        }

        #endregion

        #region Private Methods

        private int DoNothing() => 0;

        /// <summary>
        /// Invoking DoNothing in an especially costly way.
        /// </summary>
        private int DoSomething()
        {
            Type t = GetType();
            MethodInfo mi = t.GetMethod(nameof(DoNothing), BindingFlags.Instance | BindingFlags.NonPublic);
            MethodAccessor invoker = new FunctionMethodAccessor(mi); // using the internal constructor makes sure the delegates are re-created again and again
            int result = (int)invoker.Invoke(this, null);
            return result;
        }

        #endregion

        #endregion
    }
}
