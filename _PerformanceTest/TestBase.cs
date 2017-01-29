namespace _PerformanceTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    using KGySoft.Libraries;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public abstract class TestBase
    {
        protected abstract class TestOpBase
        {
            public string TestName { get; set; }
            public string RefOpName { get; set; }
            public string CheckOpName { get; set; }
            public int Iterations { get; set; }
            public int WarmUpTime { get; set; }
            public bool Collect { get; set; }
            public int Repeat { get; set; }

            protected TestOpBase()
            {
                WarmUpTime = 2000;
                Collect = true;
                Iterations = 100;
                Repeat = 1;
            }
        }

        protected class TestOperation: TestOpBase
        {
            public Action RefOp { get; set; }
            public Action CheckOp { get; set; }
        }

        protected class TestOperation<TRes>: TestOpBase where TRes : ICollection<TRes>
        {
            public Func<TRes> RefOp { get; set; }
            public Func<TRes> CheckOp { get; set; }
        }

        private IntPtr origAffinity;
        private ProcessPriorityClass origPriority;
        private ThreadPriority origThreadPrio;

        protected int CpuAffinity { get; set; }
        protected bool AdjustCpuUsage { get; set; }

        protected TestBase()
        {
            CpuAffinity = 2;
            AdjustCpuUsage = true;
        }

        protected static void CheckTestingFramework()
        {
#if NET35
            if (typeof(object).Assembly.GetName().Version != new Version(2, 0, 0, 0))
                Assert.Inconclusive("mscorlib version does not match to .NET 3.5: {0}. Add a global <TargetFrameworkVersion>v3.5</TargetFrameworkVersion> to csproj and try again", typeof(object).Assembly.GetName().Version);
#elif NET40 || NET45
            if (typeof(object).Assembly.GetName().Version != new Version(4, 0, 0, 0))
                Assert.Inconclusive("mscorlib version does not match to .NET 4.x: {0}. Add a global <TargetFrameworkVersion> to csproj and try again", typeof(object).Assembly.GetName().Version);
#endif
        }

        [TestInitialize]
        public void ClassInit()
        {
            if (!AdjustCpuUsage)
                return;

            origAffinity = Process.GetCurrentProcess().ProcessorAffinity;
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(CpuAffinity);
            origPriority = Process.GetCurrentProcess().PriorityClass;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            origThreadPrio = Thread.CurrentThread.Priority;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
        }

        [TestCleanup]
        public void ClassCleanup()
        {
            if (!AdjustCpuUsage)
                return;

            Process.GetCurrentProcess().ProcessorAffinity = origAffinity;
            Process.GetCurrentProcess().PriorityClass = origPriority;
            Thread.CurrentThread.Priority = origThreadPrio;
        }

        protected static void DoTest<TRes>(TestOperation<TRes> testOp)
            where TRes: class, ICollection<TRes>
        {
            if (testOp == null)
                throw new ArgumentNullException("testOp");

            var watch = new Stopwatch();
            var iterations = testOp.Iterations;
            var refOp = testOp.RefOp;
            var checkOp = testOp.CheckOp;

            Console.WriteLine("=========={0} (iterations: {1:N0})===========", testOp.TestName, testOp.Iterations);
            if (testOp.WarmUpTime > 0)
            {
                watch.Start();
                do
                {
                    refOp.Invoke();
                    checkOp.Invoke();
                }
                while (watch.ElapsedMilliseconds < testOp.WarmUpTime);

                watch.Reset();
            }

            if (testOp.Collect)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            TRes resultRef = null;
            watch.Start();
            for (int i = 0; i < iterations; i++)
                resultRef = refOp.Invoke();
            watch.Stop();
            TimeSpan timeRef = watch.Elapsed;

            watch.Reset();
            if (testOp.Collect)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            TRes resultCheck = null;
            watch.Start();
            for (int i = 0; i < iterations; i++)
                resultCheck = checkOp.Invoke();
            watch.Stop();
            TimeSpan timeCheck = watch.Elapsed;
            Console.WriteLine("{0} time: {1:N2} ms", testOp.RefOpName, timeRef.TotalMilliseconds);
            if (resultRef != null)
                Console.WriteLine("{0} size: {1:N} {2}s", testOp.RefOpName, resultRef.Count, typeof(TRes).GetGenericArguments()[0].Name.ToLowerInvariant());
            Console.WriteLine("{0} time: {1:N2} ms", testOp.CheckOpName, timeCheck.TotalMilliseconds);
            if (resultCheck != null)
                Console.WriteLine("{0} size: {1:N} {2}s", testOp.RefOpName, resultCheck.Count, typeof(TRes).GetGenericArguments()[0].Name.ToLowerInvariant());

            Console.WriteLine("Time performance: {0:P2}", timeCheck.TotalMilliseconds / timeRef.TotalMilliseconds);
            if (resultRef != null && resultCheck != null)
                Console.WriteLine("Size performance: {0:P2}", (double)resultCheck.Count / resultRef.Count);
            Console.WriteLine();
        }

        protected static void DoTest(TestOperation testOp)
        {
            if (testOp == null)
                throw new ArgumentNullException("testOp");

            if (testOp.CheckOp == null)
                throw new ArgumentException("CheckOp of the test is null", "testOp");

            var watch = new Stopwatch();
            var iterations = testOp.Iterations;
            var refOp = testOp.RefOp;
            var checkOp = testOp.CheckOp;
            var times = new TimeSpan[testOp.Repeat, 2];

            Console.WriteLine("=========={0} (iterations: {1:N0})===========", testOp.TestName, testOp.Iterations);
            if (testOp.WarmUpTime > 0)
            {
                watch.Start();
                do
                {
                    if (refOp != null)
                        refOp.Invoke();
                    checkOp.Invoke();
                }
                while (watch.ElapsedMilliseconds < testOp.WarmUpTime);
            }

            int timeRefMin = -1;
            int timeRefMax = -1;
            if (refOp != null)
            {
                for (int r = 0; r < testOp.Repeat; r++)
                {
                    watch.Reset();
                    if (testOp.Collect)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    }

                    watch.Start();
                    for (int i = 0; i < iterations; i++)
                        refOp.Invoke();
                    watch.Stop();
                    times[r, 0] = watch.Elapsed;
                }

                for (int r = 0; r < testOp.Repeat; r++)
                {
                    if (timeRefMin == -1 || times[r, 0] < times[timeRefMin, 0])
                        timeRefMin = r;
                    if (timeRefMax == -1 || times[r, 0] > times[timeRefMax, 0])
                        timeRefMax = r;
                }

                if (testOp.Repeat == 1)
                    Console.WriteLine("{0} time: {1:N2} ms", testOp.RefOpName, times[0, 0].TotalMilliseconds);
                else
                {
                    Console.WriteLine("{0} times:", testOp.RefOpName);
                    for (int r = 0; r < testOp.Repeat; r++)
                    {
                        Console.Write("{0:N2} ms", times[r, 0].TotalMilliseconds);
                        if (timeRefMin != timeRefMax
                            && r.In(timeRefMin, timeRefMax))
                            Console.Write("\t <---- {0}", r == timeRefMin ? "Best" : "Worst");
                        Console.WriteLine();
                    }

                    Console.WriteLine("Worst-Best difference: {0:N2} ms ({1:P2})",
                        (times[timeRefMax, 0] - times[timeRefMin, 0]).TotalMilliseconds,
                        times[timeRefMax, 0].TotalMilliseconds / times[timeRefMin, 0].TotalMilliseconds - 1);
                }

                if (times[0, 0].TotalMilliseconds / iterations < 0.00001)
                {
                    Console.WriteLine();
                    Console.WriteLine("WARNING: {0} is a too small operation to give trusted results.", testOp.RefOpName);
                    Console.WriteLine();
                }
                else if (testOp.Repeat > 1)
                    Console.WriteLine();
            }

            int timeCheckMin = -1;
            int timeCheckMax = -1;
            for (int r = 0; r < testOp.Repeat; r++)
            {
                watch.Reset();
                if (testOp.Collect)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                watch.Start();
                for (int i = 0; i < iterations; i++)
                    checkOp.Invoke();
                watch.Stop();
                times[r, 1] = watch.Elapsed;
            }

            for (int r = 0; r < testOp.Repeat; r++)
            {
                if (timeCheckMin == -1 || times[r, 1] < times[timeCheckMin, 1])
                    timeCheckMin = r;
                if (timeCheckMax == -1 || times[r, 1] > times[timeCheckMax, 1])
                    timeCheckMax = r;
            }

            if (testOp.Repeat == 1)
                Console.WriteLine("{0} time: {1:N2} ms", testOp.CheckOpName, times[0, 1].TotalMilliseconds);
            else
            {
                Console.WriteLine("{0} times:", testOp.CheckOpName);
                for (int r = 0; r < testOp.Repeat; r++)
                {
                    Console.Write("{0:N2} ms", times[r, 1].TotalMilliseconds);
                    if (timeCheckMin != timeCheckMax
                        && r.In(timeCheckMin, timeCheckMax))
                        Console.Write("\t <---- {0}", r == timeCheckMin ? "Best" : "Worst");
                    Console.WriteLine();
                }

                Console.WriteLine("Worst-Best difference: {0:N2} ms ({1:P2})",
                    (times[timeCheckMax, 1] - times[timeCheckMin, 1]).TotalMilliseconds,
                    times[timeCheckMax, 1].TotalMilliseconds / times[timeCheckMin, 1].TotalMilliseconds - 1);
            }

            if (times[0, 1].TotalMilliseconds / iterations < 0.00001)
            {
                Console.WriteLine();
                Console.WriteLine("WARNING: {0} is a too small operation to give trusted results.", testOp.CheckOpName);
            }

            if (timeRefMin != -1)
            {
                Console.WriteLine();
                if (timeRefMin == timeRefMax && timeCheckMin == timeCheckMax)
                    Console.WriteLine("Time performance: {0:P2}", times[0, 1].TotalMilliseconds / times[0, 0].TotalMilliseconds);
                else
                    Console.WriteLine("Time performance: {0:P2} - {1:P2}",
                        times[timeCheckMin, 1].TotalMilliseconds / times[timeRefMax, 0].TotalMilliseconds,
                        times[timeCheckMax, 1].TotalMilliseconds / times[timeRefMin, 0].TotalMilliseconds);
            }
            Console.WriteLine();
        }
    }
}
