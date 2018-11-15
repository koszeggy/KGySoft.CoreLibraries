using System;
using System.Diagnostics;
using System.Reflection;
using KGySoft.Diagnostics;
using KGySoft.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _PerformanceTest.Tests.Diagnostics
{
    [TestClass]
    public class ProfilerPerformanceTest : TestBase
    {
        /// <summary>
        /// This test just compares the different profiling methods:
        /// - The base solution (invoking delegates)
        /// - The Profiler's solution (enclosing into using)
        /// - Direct measurement
        /// We want to just determinate whether the delagate call or using a disposable measurement class have too large
        /// overload to test micro measurements.
        /// </summary>
        [TestMethod]
        public void CompareProfilingWaysCheapOperation()
        {
            const int iterations = 10000000;
            var test = new TestOperation
                {
                    TestName = "DoTest performance without warmup",
                    TestOpName = "DoNothing",
                    TestOperation = () => DoNothing(),
                    WarmUpTime = 0,
                    Iterations = iterations,
                    Repeat = 5
                };

            // 1. DoTest without warmup
            test.DoTest();

            test.TestName = "DoTest performance with warmup";
            test.WarmUpTime = 1000;

            // 2. DoTest with warmup
            test.DoTest();

            // 3. Direct test
            Console.WriteLine("===========Direct measurement test===============");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // warming up
            for (int i = 0; i < iterations; i++)
            {
                DoNothing();
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                DoNothing();
            }
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
                using (Profiler.Measure("ProfilerTest", "WarmingUp")) { }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            for (int i = 0; i < iterations; i++)
            {
                using (Profiler.Measure("ProfilerTest", "SelfCostWithoutOp")) { }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            using (Profiler.Measure("ProfilerTest", "TotalWithSubMeasures"))
            {
                for (int i = 0; i < iterations; i++)
                {
                    using (Profiler.Measure("ProfilerTest", "DoNothingCall"))
                    {
                        DoNothing();
                    }
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            using (Profiler.Measure("ProfilerTest", "PureTotal"))
            {
                for (int i = 0; i < iterations; i++)
                {
                    DoNothing();
                }
            }

            foreach (IMeasureItem item in Profiler.GetMeasurementResults("ProfilerTest"))
            {
                Console.WriteLine("{0}: {1:N2} ms", item.Operation, item.TotalElapsed.TotalMilliseconds);
            }

            Console.WriteLine();
            Console.WriteLine("PureTotal should be nearly the same as Direct measurement, and PureTotal should be DoNothingCall - SelfCostWithoutOp");
            Console.WriteLine("TotalWithSubMeasures - DoNothingCall = the cost of the Profiler itself (SelfCostWithoutOp does not contain the administration costs of the results)");
        }

        /// <summary>
        /// Compares the profiling ways of an expensive operation.
        /// There should be no significant difference between the different ways.
        /// </summary>
        [TestMethod]
        public void CompareProfilingWaysExpensiveOperation()
        {
            const int iterations = 10000;
            var test = new TestOperation
            {
                TestName = "DoTest performance without warmup",
                TestOpName = "DoSomething",
                TestOperation = () => DoSomething(),
                WarmUpTime = 0,
                Iterations = iterations,
                Repeat = 5
            };

            // 1. DoTest without warmup
            test.DoTest();

            test.TestName = "DoTest performance with warmup";
            test.WarmUpTime = 1000;

            // 2. DoTest with warmup
            test.DoTest();

            // 3. Direct test
            Console.WriteLine("===========Direct measurement test===============");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // warming up
            for (int i = 0; i < iterations; i++)
            {
                DoSomething();
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                DoSomething();
            }
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
                using (Profiler.Measure("ProfilerTest", "WarmingUp")) { }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            for (int i = 0; i < iterations; i++)
            {
                using (Profiler.Measure("ProfilerTest", "SelfCostWithoutOp")) { }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            using (Profiler.Measure("ProfilerTest", "TotalWithSubMeasures"))
            {
                for (int i = 0; i < iterations; i++)
                {
                    using (Profiler.Measure("ProfilerTest", "DoSomethingCall"))
                    {
                        DoSomething();
                    }
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            using (Profiler.Measure("ProfilerTest", "PureTotal"))
            {
                for (int i = 0; i < iterations; i++)
                {
                    DoSomething();
                }
            }

            foreach (IMeasureItem item in Profiler.GetMeasurementResults("ProfilerTest"))
            {
                Console.WriteLine("{0}: {1:N2} ms", item.Operation, item.TotalElapsed.TotalMilliseconds);
            }

            Console.WriteLine();
            Console.WriteLine("In case of a costly operation the DirectTotal < PureTotal < DoTest < DoSomethingCall < TotalWithSubMeasures should have nearly the same value.");
        }

        private int DoNothing()
        {
            return 0;
        }

        /// <summary>
        /// Invoking DoNothing via an especially non-performant way.
        /// </summary>
        private int DoSomething()
        {
            Type t = this.GetType();
            MethodInfo mi = t.GetMethod("DoNothing", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInvoker invoker = new FunctionInvoker(mi);
            int result = (int)invoker.Invoke(this, null);
            return result;
        }

    }
}
