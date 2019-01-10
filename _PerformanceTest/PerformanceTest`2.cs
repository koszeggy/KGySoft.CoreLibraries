#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PerformanceTest`2.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace _PerformanceTest
{
    internal abstract class PerformanceTest<TDelegate, TResult>
    {
        #region Nested classes

        #region TestCase class

        private class TestCase
        {
            #region Fields

            internal TDelegate Case;
            internal string Name;
            internal TestResult TestResult;

            #endregion
        }

        #endregion

        #region TestResult class

        private class TestResult
        {
            #region Fields

            internal readonly List<TimeSpan> Times = new List<TimeSpan>();
            internal readonly List<int> Iterations = new List<int>();
            internal readonly List<double> AverageIterationsPerTestTime = new List<double>();
            internal TResult Result;
            internal int IndexBest;
            internal int IndexWorst;

            #endregion

            #region Properties

            internal TimeSpan TotalTime => Times.Aggregate(TimeSpan.Zero, (acc, curr) => acc + curr);
            internal TimeSpan AverageTime => new TimeSpan(TotalTime.Ticks / Times.Count);
            internal double AverageIteration => AverageIterationsPerTestTime.Average();

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        private readonly List<TestCase> cases = new List<TestCase>();

        private IntPtr origAffinity;
        private ProcessPriorityClass origPriority;
        private ThreadPriority origThreadPrio;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the test.
        /// </summary>
        public string TestName { get; set; }

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
        /// Gets or sets the warming up time in milliseconds before each cases.
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
        /// Gets or sets whether the test configuration should be dumped.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public bool DumpConfig { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the last result of the test operations should be dumped.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        public bool DumpResult { get; set; }

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

        /// <summary>
        /// Gets or sets whether the results should be sorted by the size of the produced result instead of time results.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        public bool SortBySize { get; set; }

        #endregion

        #region Methods

        #region Static Methods

        private static int GetLength(TResult result)
        {
            switch (result)
            {
                case ICollection collection:
                    return collection.Count;
                case string s:
                    return s.Length;
                case IEnumerable e:
                    return e.Cast<object>().Count();
                default:
                    return Reflector.SizeOf<TResult>();
            }
        }

        private static string GetUnit() => (from i in typeof(TResult).GetInterfaces() where i.IsGenericTypeOf(typeof(IEnumerable<>)) select i.GetGenericArguments()[0].Name.ToLowerInvariant() + "s").FirstOrDefault() ?? "bytes";

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

        #endregion

        #region Instance Methods

        #region Public Methods

        public PerformanceTest<TDelegate, TResult> AddCase(TDelegate testCase, string name = null)
        {
            if (testCase == null)
                throw new ArgumentNullException(nameof(testCase));
            cases.Add(new TestCase { Case = testCase, Name = name ?? $"Case #{cases.Count + 1}" });
            return this;
        }

        public void DoTest()
        {
            if (cases == null)
                throw new InvalidOperationException("No test cases are added");
            Initialize();
            try
            {
                DoTestCases();
                SortResults();
                DumpResults();
            }
            finally
            {
                TearDown();
            }
        }

        #endregion

        #region Protected Methods

        protected abstract TResult Invoke(TDelegate del);

        #endregion

        #region Private Methods

        private void Initialize()
        {
            PerformanceTest.CheckTestingFramework();
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

        private void WarmUp(TDelegate testCase)
        {
            if (WarmUpTime <= 0)
                return;

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            do
                Invoke(testCase);
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
                testCase.TestResult = testResult;
                WarmUp(testCase.Case);

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
                    if (Iterations > 0)
                    {
                        if (r == 0 || testResult.Times[r] < testResult.Times[testResult.IndexBest])
                            testResult.IndexBest = r;
                        if (r == 0 || testResult.Times[r] > testResult.Times[testResult.IndexWorst])
                            testResult.IndexWorst = r;
                    }
                    else
                    {
                        if (r == 0 || testResult.AverageIterationsPerTestTime[r] > testResult.AverageIterationsPerTestTime[testResult.IndexBest])
                            testResult.IndexBest = r;
                        if (r == 0 || testResult.AverageIterationsPerTestTime[r] < testResult.AverageIterationsPerTestTime[testResult.IndexWorst])
                            testResult.IndexWorst = r;
                    }
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
            testResult.AverageIterationsPerTestTime.Add(iterations * (TestTime / stopwatch.Elapsed.TotalMilliseconds));
            testResult.Result = result;
        }

        private void SortResults()
        {
            Comparison<TestCase> comparison = SortBySize
                    ? (x, y) => Comparer<int>.Default.Compare(GetLength(x.TestResult.Result), GetLength(y.TestResult.Result))
                        : Iterations > 0
                        ? (x, y) => Comparer<TimeSpan>.Default.Compare(x.TestResult.AverageTime, y.TestResult.AverageTime)
                            : (Comparison<TestCase>)((x, y) => -Comparer<double>.Default.Compare(x.TestResult.AverageIteration, y.TestResult.AverageIteration));
            cases.Sort(comparison);
        }

        private void DumpResults()
        {
            string DumpDiff(double currentValue, double baseValue, string unit = null)
            {
                double diff = currentValue - baseValue;
                if (diff.Equals(0d))
                    return "No difference";

                string sign = diff > 0 ? "+" : null;
                unit = unit == null ? null : $" {unit}";
                return $"{sign}{diff:N2}{unit} / {currentValue / baseValue:P2}";
            }

            Console.WriteLine($"==[{TestName ?? "Performance Test"} Results]================================================");
            if (DumpConfig)
            {
                Console.WriteLine(Iterations > 0 ? $"Iterations: {Iterations}" : $"Test Time: {TestTime:N0} ms");
                if (WarmUpTime > 0)
                    Console.WriteLine($"Warming up time: {WarmUpTime:N0} ms");
                Console.WriteLine($"Test cases: {cases.Count}");
                if (Repeat > 1)
                    Console.WriteLine($"Repeats: {Repeat}");
                Console.WriteLine($"Calling GC.Collect: {Collect}");
                Console.WriteLine($"CPU usage adjusted: {AdjustCpuUsage}");
                if (AdjustCpuUsage)
                    Console.WriteLine($"CPU affinity: {CpuAffinity}");
                if (cases.Count > 1)
                    Console.WriteLine($"Cases are sorted by {(SortBySize ? "size (shortest first)" : Iterations > 0 ? "time (quickest first)" : "fulfilled iterations (most first)")}");
                Console.WriteLine("--------------------------------------------------");
            }

            TestResult baseLine = cases[0].TestResult;
            int baseLength = GetLength(baseLine.Result);
            for (int i = 0; i < cases.Count; i++)
            {
                // Headline
                TestCase testCase = cases[i];
                if (cases.Count > 1)
                    Console.Write($"{i + 1}. ");
                Console.Write(testCase.Name);
                TestResult result = testCase.TestResult;

                if (Iterations > 0)
                {
                    Console.Write($" average time: {result.AverageTime.TotalMilliseconds:N2} ms");
                    if (i > 0)
                        Console.Write($" ({DumpDiff(result.AverageTime.TotalMilliseconds, baseLine.AverageTime.TotalMilliseconds, "ms")})");
                }
                else
                {
                    int totalIterations = result.Iterations.Sum();
                    Console.Write($" {totalIterations:N0} iterations in {result.TotalTime.TotalMilliseconds:N2} ms. Adjusted for {TestTime:N0} ms: {result.AverageIteration:N2}");
                    if (i > 0)
                        Console.Write($" ({DumpDiff(result.AverageIteration, baseLine.AverageIteration)})");
                }

                Console.WriteLine();

                // Repeats
                if (Repeat > 1)
                {
                    for (int r = 0; r < Repeat; r++)
                    {
                        Console.Write($"  #{r + 1,-2} {(Iterations > 0 ? $"{result.Times[r].TotalMilliseconds,13:N2} ms" : $"{result.Iterations[r]:N0} iterations in {result.Times[r].TotalMilliseconds:N2} ms. Adjusted: {result.AverageIterationsPerTestTime[r]:N2}")}");
                        if (result.IndexBest != result.IndexWorst && r.In(result.IndexBest, result.IndexWorst))
                            Console.Write($"\t <---- {(r == result.IndexBest ? "Best" : "Worst")}");
                        Console.WriteLine();
                    }

                    Console.Write("  Worst-Best difference: ");
                    Console.WriteLine(Iterations > 0 ?
                            $"{(result.Times[result.IndexWorst] - result.Times[result.IndexBest]).TotalMilliseconds:N2} ms ({result.Times[result.IndexWorst].TotalMilliseconds / result.Times[result.IndexBest].TotalMilliseconds - 1:P2})"
                            : $"{(result.AverageIterationsPerTestTime[result.IndexBest] - result.AverageIterationsPerTestTime[result.IndexWorst]):N2} ({result.AverageIterationsPerTestTime[result.IndexBest] / result.AverageIterationsPerTestTime[result.IndexWorst] - 1:P2})");
                }

                // Result
                if (result.Result != null)
                {
                    int caseLength = GetLength(result.Result);
                    Console.Write($"  Result size: {caseLength:N0} {GetUnit()}");
                    if (i > 0)
                        Console.Write($" ({DumpDiff(caseLength, baseLength, GetUnit())})");
                    Console.WriteLine();

                    if (DumpResult)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Dumped result:");
                        Dump(result.Result);
                        Console.WriteLine();
                    }
                }
            }

            Console.WriteLine();
        }

        #endregion

        #endregion

        #endregion
    }
}
