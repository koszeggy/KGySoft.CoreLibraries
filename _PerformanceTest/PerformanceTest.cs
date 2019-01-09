using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KGySoft.CoreLibraries;
using NUnit.Framework;

namespace _PerformanceTest
{
    internal abstract class PerformanceTest<TDelegate, TResult>
    {
        /// <summary>
        /// Gets or sets number of iterations of test cases. If greater than zero, then <see cref="TestTime"/> is ignored.
        /// <br/>Default value: <c>0</c>.
        /// </summary>
        public int Iterations { get; set; }

        /// <summary>
        /// Gets or sets the test time in milliseconds.
        /// <br/>Default value: <c>2000</c>.
        /// </summary>
        public int TestTime { get; set; } = 2000;

        /// <summary>
        /// Gets or sets the warming up time in milliseconds.
        /// <br/>Default value: <c>2000</c>.
        /// </summary>
        public int WarmUpTime { get; set; } = 2000;

        /// <summary>
        /// Gets or sets whether <see cref="GC.Collect()">GC.Collect</see> should be called before running the test cases.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public bool Collect { get; set; } = true;

        /// <summary>
        /// Gets or sets how many times the test cases sould be repeated.
        /// <br/>Default value: <c>1</c>.
        /// </summary>
        public int Repeat { get; set; } = 1;

        /// <summary>
        /// Gets or sets whether the last result of the test operations should be dumped.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        public bool DumpResult { get; set; } = false;

        /// <summary>
        /// Gets or sets the CPU affinity. If <see cref="AdjustCpuUsage"/> is <see langword="false"/>, then this property is ignored.
        /// <br/>Default value: <c>2</c>.
        /// </summary>
        public int CpuAffinity { get; set; } = 2;

        /// <summary>
        /// Gets or sets whether the CPU usage should be adjusted for the test.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public bool AdjustCpuUsage { get; set; } = true;

        private readonly List<TestCase> cases = new List<TestCase>();
        private IntPtr origAffinity;
        private ProcessPriorityClass origPriority;
        private ThreadPriority origThreadPrio;

        private class TestCase
        {
            internal TDelegate Case;
            internal string Name;
            internal TestResult Result;
        }

        private class TestResult
        {
            internal readonly List<TimeSpan> Times = new List<TimeSpan>();
            internal readonly List<int> Iterations = new List<int>();
            internal readonly List<decimal> AverageIterationsPerTestTime = new List<decimal>();
            internal TResult Result;
            internal int IndexBest;
            internal int IndexWorst;
            internal TimeSpan AverageTime;
            internal decimal AverageIteration;
        }

        public PerformanceTest<TDelegate, TResult> AddCase(TDelegate testCase, string name = null)
        {
            if (testCase == null)
                throw new ArgumentNullException(nameof(testCase));
            cases.Add(new TestCase { Case = testCase, Name = name ?? $"Case #{cases.Count + 1}" });
            return this;
        }

        protected abstract TResult Invoke(TDelegate del);

        public void DoTest()
        {
            if (cases == null)
                throw new InvalidOperationException("No test cases are added");
            Initialize();
            try
            {
                WarmUp();
                DoTestCases();
                SortResults();
                DumpResults();
            }
            finally
            {
                TearDown();
            }
        }

        private void Initialize()
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

        private void TearDown()
        {
            if (!AdjustCpuUsage)
                return;

            Process.GetCurrentProcess().ProcessorAffinity = origAffinity;
            Process.GetCurrentProcess().PriorityClass = origPriority;
            Thread.CurrentThread.Priority = origThreadPrio;
        }

        private void WarmUp()
        {
            if (WarmUpTime <= 0)
                return;

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            do
            {
                for (int i = 0; i < cases.Count; i++)
                    Invoke(cases[i].Case);
            }
            while (stopwatch.ElapsedMilliseconds < WarmUpTime);
        }

        private void DoCollect()
        {
            if (!Collect)
                return;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void DoTestCases()
        {
            for (int c = 0; c < cases.Count; c++)
            {
                var testCase = cases[c];
                var testResult = new TestResult();
                testCase.Result = testResult;

                for (int r = 0; r < Repeat; r++)
                {
                    DoCollect();
                    if (Iterations > 0)
                        DoTestByIterations(testCase.Case, testResult);
                    else
                        DoTestByTime(testCase.Case, testResult);
                }

                for (int r = 0; r < Repeat; r++)
                {
                    if (r == 0 || (double)testResult.Times[r].Ticks / testResult.Iterations[r] < (double)testResult.Times[testResult.IndexBest].Ticks / testResult.Iterations[testResult.IndexBest])
                        testResult.IndexBest = r;
                    if (r == 0 || (double)testResult.Times[r].Ticks / testResult.Iterations[r] > (double)testResult.Times[testResult.IndexWorst].Ticks / testResult.Iterations[testResult.IndexWorst])
                        testResult.IndexWorst = r;
                }
            }
        }

        private void DoTestByIterations(TDelegate testCase, TestResult testResult)
        {
            var stopwatch = new Stopwatch();
            TResult result = default;
            int iterations = Iterations;
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
                result = Invoke(testCase);
            stopwatch.Stop();
            testResult.Times.Add(stopwatch.Elapsed);
            testResult.Iterations.Add(iterations);
            testResult.Result = result;
            testResult.AverageIteration = iterations;
            testResult.AverageTime = testResult.Times.Aggregate(TimeSpan.Zero, (acc, curr) => acc + curr, sum => new TimeSpan(sum.Ticks / Repeat));
        }

        private void DoTestByTime(TDelegate testCase, TestResult testResult)
        {
            var stopwatch = new Stopwatch();
            TResult result;
            int iterations = 0;
            stopwatch.Start();
            do
            {
                iterations++;
                result = Invoke(testCase);
            } while (stopwatch.ElapsedMilliseconds < TestTime);
            stopwatch.Stop();
            testResult.Times.Add(stopwatch.Elapsed);
            testResult.Iterations.Add(iterations);
            testResult.AverageIterationsPerTestTime.Add(iterations * ((decimal)stopwatch.ElapsedTicks / (TestTime * TimeSpan.TicksPerMillisecond)));
            testResult.Result = result;
            testResult.AverageIteration = testResult.AverageIterationsPerTestTime.Average();
            testResult.AverageTime = testResult.Times.Aggregate(TimeSpan.Zero, (acc, curr) => acc + curr, sum => new TimeSpan(sum.Ticks / Repeat));
        }

        private void SortResults()
        {
            Comparison<TestCase> comparison = Iterations > 0 
                ? (Comparison<TestCase>)((x, y) => Comparer<TimeSpan>.Default.Compare(x.Result.AverageTime, y.Result.AverageTime)) 
                : (x, y) => -Comparer<decimal>.Default.Compare(x.Result.AverageIteration, y.Result.AverageIteration);
            cases.Sort(comparison);
        }

        private void DumpResults()
        {
            // TODO: Config
            TestResult baseLine = cases[0].Result;
            int baseLength = GetLength(baseLine.Result);
            for (int i = 0; i < cases.Count; i++)
            {
                // Headline
                TestCase testCase = cases[i];
                if (cases.Count > 1)
                    Console.Write($"#{i + 1} ");
                Console.Write(testCase.Name);
                TestResult result = testCase.Result;

                if (Iterations > 0)
                {
                    Console.Write($" average time: {result.AverageTime.TotalMilliseconds:N2} ms");
                    if (i > 0)
                        Console.Write($" (+{result.AverageTime.TotalMilliseconds - baseLine.AverageTime.TotalMilliseconds:N2} ms / {result.AverageTime.TotalMilliseconds / baseLine.AverageTime.TotalMilliseconds:P2})");
                }
                else
                {
                    Console.Write($" average runs: {result.AverageIteration:N2}");
                    if (i > 0)
                        Console.Write($" ({result.AverageIteration - baseLine.AverageIteration:N2} / {result.AverageIteration / baseLine.AverageIteration:P2})");
                }

                Console.WriteLine();

                // Repeats
                if (Repeat > 1)
                {
                    for (int r = 0; r < Repeat; r++)
                    {
                        Console.Write($"  {r + 1,3}. {(Iterations > 0 ? $"{result.Times[r].TotalMilliseconds,10:N2} ms" : $"{result.AverageIterationsPerTestTime[r],10:N2}")}");
                        if (result.IndexBest != result.IndexWorst && r.In(result.IndexBest, result.IndexWorst))
                            Console.Write($"\t <---- {(r == result.IndexBest ? "Best" : "Worst")}");
                        Console.WriteLine();
                    }

                    Console.Write("Worst-Best difference: ");
                    Console.WriteLine(Iterations > 0 ?
                        $"{(result.Times[result.IndexWorst] - result.Times[result.IndexBest]).TotalMilliseconds:N2} ms ({result.Times[result.IndexWorst].TotalMilliseconds / result.Times[result.IndexBest].TotalMilliseconds - 1:P2})" 
                        : $"{(result.AverageIterationsPerTestTime[result.IndexBest] - result.AverageIterationsPerTestTime[result.IndexWorst]):N2} ({result.AverageIterationsPerTestTime[result.IndexBest] / result.AverageIterationsPerTestTime[result.IndexWorst] - 1:P2})");
                }

                // Result
                if (result.Result != null)
                {
                    int caseLength = GetLength(result.Result);
                    Console.Write($"Result size: {caseLength:N0} {GetUnit()}");
                    if (i > 0)
                        Console.Write($" {(double)caseLength / baseLength:P2}");
                    Console.WriteLine();

                    if (DumpResult)
                    {
                        Console.WriteLine();
                        Dump(result.Result);
                    }
                }

                if (result.AverageTime.TotalMilliseconds < 0.00001)
                {
                    Console.WriteLine();
                    Console.WriteLine($"WARNING: {testCase.Name} is a too small operation to give trusted results.");
                    Console.WriteLine();
                }
            }
        }

        private static long GetLength(TResult result)
        {
            switch (result)
            {
                case ICollection collection:
                    return collection.Count;
                case string s:
                    return s.Length;
                case IEnumerable e:
                    return e.Cast<object>().Count();
                case IConvertible convertible:
                    return convertible.ToInt64(null);
                default:
                    return -1;
            }
        }

        private static string GetUnit() => (from i in typeof(TResult).GetInterfaces() where i.IsGenericTypeOf(typeof(IEnumerable<>)) select i.GetGenericArguments()[0].Name.ToLowerInvariant() + "s").FirstOrDefault();

        private static void Dump(TResult result)
        {
            const char square = '□';
            switch (result)
            {
                case byte[] bytes:
                    Console.WriteLine(Encoding.Default.GetString(bytes).Replace('\0', square));
                    break;
                case string s:
                    Console.WriteLine(s.Replace('\0', square));
                    break;
                case IEnumerable e:
                    Console.WriteLine(String.Join(", ", e.Cast<object>()
#if NET35
                        .Select(o => o.ToString()).ToArray()
#endif
                    ));
                    break;
                default:
                    Console.WriteLine(result);
                    break;
            }
        }

    }

    internal class PerformanceTest : PerformanceTest<Action, object>
    {
        protected override object Invoke(Action del)
        {
            del.Invoke();
            return null;
        }
    }

    internal class PerformanceTest<TResult> : PerformanceTest<Func<TResult>, TResult>
    {
        protected override TResult Invoke(Func<TResult> del) => del.Invoke();
    }
}
