using System.Collections;
using System.Linq;
using System.Text;

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
        protected abstract class TestOpBase<TDelegate>
         // TODO: C# 7.3: where TDelegate : Delegate
        {
            public string TestName { get; set; }
            public string RefOpName { get; set; }
            public string TestOpName { get; set; }
            public int Iterations { get; set; } = 100;
            public int WarmUpTime { get; set; } = 2000;
            public bool Collect { get; set; } = true;
            public int Repeat { get; set; } = 1;
            public TDelegate ReferenceOperation { get; set; }
            public TDelegate TestOperation { get; set; }
            public bool DumpResult { get; set; } = true;

            protected abstract object InvokeReferenceOperation();
            protected abstract object InvokeTestOperation();

            protected TestOpBase()
            {
            }

            public void DoTest()
            {
                if (TestOperation == null)
                    throw new InvalidOperationException($"{nameof(TestOperation)} of the test is null");

                var watch = new Stopwatch();
                var iterations = Iterations;
                var times = new TimeSpan[Repeat, 2];
                bool hasRefOp = ReferenceOperation != null;

                if (TestName != null)
                    Console.WriteLine($"=========={TestName} (iterations: {Iterations:N0})===========");
                if (WarmUpTime > 0)
                {
                    watch.Start();
                    do
                    {
                        if (hasRefOp)
                            InvokeReferenceOperation();
                        InvokeTestOperation();
                    }
                    while (watch.ElapsedMilliseconds < WarmUpTime);
                }

                int timeRefMin = -1;
                int timeRefMax = -1;
                object refResult = null;
                int refLength = -1;
                if (hasRefOp)
                {
                    for (int r = 0; r < Repeat; r++)
                    {
                        watch.Reset();
                        if (Collect)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            GC.Collect();
                        }

                        watch.Start();
                        for (int i = 0; i < iterations; i++)
                            refResult = InvokeReferenceOperation();
                        watch.Stop();
                        times[r, 0] = watch.Elapsed;
                    }

                    for (int r = 0; r < Repeat; r++)
                    {
                        if (timeRefMin == -1 || times[r, 0] < times[timeRefMin, 0])
                            timeRefMin = r;
                        if (timeRefMax == -1 || times[r, 0] > times[timeRefMax, 0])
                            timeRefMax = r;
                    }

                    var refOpName = RefOpName ?? "Reference Operation";
                    if (Repeat == 1)
                        Console.WriteLine($"{refOpName} time: {times[0, 0].TotalMilliseconds:N2} ms");
                    else
                    {
                        Console.WriteLine("{0} times:", refOpName);
                        TimeSpan total = TimeSpan.Zero;
                        for (int r = 0; r < Repeat; r++)
                        {
                            Console.Write("{0,10:N2} ms", times[r, 0].TotalMilliseconds);
                            total += times[r, 0];
                            if (timeRefMin != timeRefMax
                                && r.In(timeRefMin, timeRefMax))
                                Console.Write($"\t<---- {(r == timeRefMin ? "Best" : "Worst")}");
                            Console.WriteLine();
                        }

                        Console.WriteLine($"Worst-Best difference: {(times[timeRefMax, 0] - times[timeRefMin, 0]).TotalMilliseconds:N2} ms ({times[timeRefMax, 0].TotalMilliseconds / times[timeRefMin, 0].TotalMilliseconds - 1:P2})");
                        Console.WriteLine($"Average time: {total.TotalMilliseconds / Repeat:N2} ms");
                    }

                    if (refResult != null)
                    {
                        refLength = GetLength(refResult);
                        Console.WriteLine($"{refOpName} size: {refLength:N0} {GetUnit(refResult)}");
                        if (DumpResult)
                        {
                            Console.WriteLine();
                            Dump(refResult);
                        }
                    }

                    if (times[0, 0].TotalMilliseconds / iterations < 0.00001)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"WARNING: {refOpName} is a too small operation to give trusted results.");
                        Console.WriteLine();
                    }
                    else if (Repeat > 1 || refResult != null && DumpResult)
                        Console.WriteLine();
                }

                int timeCheckMin = -1;
                int timeCheckMax = -1;
                object testResult = null;
                int testLength = -1;
                for (int r = 0; r < Repeat; r++)
                {
                    watch.Reset();
                    if (Collect)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    }

                    watch.Start();
                    for (int i = 0; i < iterations; i++)
                        testResult = InvokeTestOperation();
                    watch.Stop();
                    times[r, 1] = watch.Elapsed;
                }

                for (int r = 0; r < Repeat; r++)
                {
                    if (timeCheckMin == -1 || times[r, 1] < times[timeCheckMin, 1])
                        timeCheckMin = r;
                    if (timeCheckMax == -1 || times[r, 1] > times[timeCheckMax, 1])
                        timeCheckMax = r;
                }

                var testOpName = TestOpName ?? "Test Operation";
                if (Repeat == 1)
                    Console.WriteLine($"{testOpName} time: {times[0, 1].TotalMilliseconds:N2} ms");
                else
                {
                    Console.WriteLine($"{testOpName} times:");
                    TimeSpan total = TimeSpan.Zero;
                    for (int r = 0; r < Repeat; r++)
                    {
                        Console.Write($"{times[r, 1].TotalMilliseconds,10:N2} ms");
                        total += times[r, 1];
                        if (timeCheckMin != timeCheckMax
                            && r.In(timeCheckMin, timeCheckMax))
                            Console.Write($"\t <---- {(r == timeCheckMin ? "Best" : "Worst")}");
                        Console.WriteLine();
                    }

                    Console.WriteLine($"Worst-Best difference: {(times[timeCheckMax, 1] - times[timeCheckMin, 1]).TotalMilliseconds:N2} ms ({times[timeCheckMax, 1].TotalMilliseconds / times[timeCheckMin, 1].TotalMilliseconds - 1:P2})");
                    Console.WriteLine($"Average time: {total.TotalMilliseconds / Repeat:N2} ms");
                }

                if (testResult != null)
                {
                    testLength = GetLength(testResult);
                    Console.WriteLine($"{testOpName} size: {testLength:N0} {GetUnit(testResult)}");
                    if (DumpResult)
                    {
                        Console.WriteLine();
                        Dump(testResult);
                    }
                }

                if (times[0, 1].TotalMilliseconds / iterations < 0.00001)
                {
                    Console.WriteLine();
                    Console.WriteLine($"WARNING: {testOpName} is a too small operation to give trusted results.");
                    Console.WriteLine();
                }
                else if (Repeat > 1 || testResult != null && DumpResult)
                    Console.WriteLine();

                if (timeRefMin != -1)
                {
                    if (timeRefMin == timeRefMax && timeCheckMin == timeCheckMax)
                        Console.WriteLine($"Time performance: {times[0, 1].TotalMilliseconds / times[0, 0].TotalMilliseconds:P2}");
                    else
                        Console.WriteLine($"Time performance: {times[timeCheckMin, 1].TotalMilliseconds / times[timeRefMax, 0].TotalMilliseconds:P2} - {times[timeCheckMax, 1].TotalMilliseconds / times[timeRefMin, 0].TotalMilliseconds:P2}");
                }

                if (refLength >= 0 && testLength >= 0)
                    Console.WriteLine($"Size performance: {(double)testLength / refLength:P2}");
                Console.WriteLine();
            }

            private static int GetLength(object result)
            {
                if (result is ICollection collection)
                    return collection.Count;
                if (result is string s)
                    return s.Length;
                if (result is IEnumerable e)
                    return e.Cast<object>().Count();
                return -1;
            }

            private string GetUnit(object result) => (from i in result.GetType().GetInterfaces() where i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>) select i.GetGenericArguments()[0].Name.ToLowerInvariant() + "s").FirstOrDefault();

            private void Dump(object result)
            {
                const char square = '□';
                if (result is byte[] bytes)
                    Console.WriteLine(Encoding.Default.GetString(bytes).Replace('\0', square));
                else if (result is string s)
                    Console.WriteLine(s.Replace('\0', square));
                else if (result is IEnumerable e)
                    Console.WriteLine(String.Join(", ", e.Cast<object>()));
                else
                    Console.WriteLine(result);
            }
        }

        protected class TestOperation: TestOpBase<Action>
        {
            protected override object InvokeReferenceOperation()
            {
                ReferenceOperation.Invoke();
                return null;
            }

            protected override object InvokeTestOperation()
            {
                TestOperation.Invoke();
                return null;
            }
        }

        protected class TestOperation<TResult>: TestOpBase<Func<TResult>>
        {
            protected override object InvokeReferenceOperation() => ReferenceOperation.Invoke();
            protected override object InvokeTestOperation() => TestOperation.Invoke();
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
    }
}
